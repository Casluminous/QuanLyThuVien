using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.RateLimiting;
using QuanLyThuVien.Chat.Contracts;
using QuanLyThuVien.ChatApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("chat", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.AddSingleton<CatalogService>();
builder.Services.AddSingleton<OpenAiGateway>();
builder.Services.AddSingleton<ChatConcurrencyGate>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/", () => Results.Ok(new
{
    service = "QuanLyThuVien.ChatApi",
    status = "running",
    health = "/health",
    chat = "/api/chat/stream",
    message = "Đây là API nội bộ; hãy mở QuanLyThuVien.exe để sử dụng giao diện."
}));

app.MapGet("/health", async (CatalogService catalog, OpenAiGateway ai, CancellationToken cancellationToken) =>
{
    bool databaseAvailable = await catalog.CanConnectAsync(cancellationToken);
    var health = new
    {
        status = databaseAvailable && ai.IsConfigured ? "ok" : "degraded",
        databaseAvailable,
        openAiConfigured = ai.IsConfigured,
        utc = DateTimeOffset.UtcNow
    };
    return databaseAvailable && ai.IsConfigured
        ? Results.Ok(health)
        : Results.Json(health, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapPost("/api/chat/stream", async (HttpContext http, ChatRequest? request, CatalogService catalog, OpenAiGateway ai, ChatConcurrencyGate concurrencyGate, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    if (!IsAllowed(http.User, configuration["Chat:AdGroup"]))
        return Results.Forbid();
    if (request is null)
        return Results.BadRequest(new { message = "Yêu cầu chat không hợp lệ." });
    string? validationError = ChatRequestValidator.Validate(request);
    if (validationError != null)
        return Results.BadRequest(new { message = validationError });
    if (!ai.IsConfigured)
        return Results.Json(
            new { code = "openai_not_configured", message = "Máy chủ trợ lý chưa được cấu hình OPENAI_API_KEY." },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    request = request with { History = ChatRequestValidator.LimitHistory(request.History) };

    using IDisposable? concurrencyLease = await concurrencyGate.TryEnterAsync(cancellationToken);
    if (concurrencyLease is null)
        return Results.Json(new { message = "Trợ lý đang xử lý tối đa hai yêu cầu. Vui lòng thử lại sau." }, statusCode: StatusCodes.Status429TooManyRequests);

    var stopwatch = Stopwatch.StartNew();
    string userHash = HashIdentity(http.User.Identity?.Name);
    http.Response.StatusCode = StatusCodes.Status200OK;
    http.Response.ContentType = "text/event-stream; charset=utf-8";
    http.Response.Headers.CacheControl = "no-cache";
    http.Response.Headers.Connection = "keep-alive";

    async Task Emit(ChatStreamEvent value)
    {
        string json = JsonSerializer.Serialize(value, JsonDefaults.Options);
        await http.Response.WriteAsync($"event: {value.Type}\ndata: {json}\n\n", cancellationToken);
        await http.Response.Body.FlushAsync(cancellationToken);
    }

    try
    {
        bool onlyAvailable = request.Message.Contains("còn", StringComparison.OrdinalIgnoreCase)
            || request.Message.Contains("trong kho", StringComparison.OrdinalIgnoreCase)
            || request.Message.Contains("đang có", StringComparison.OrdinalIgnoreCase);
        var books = await FindCatalogMatchesAsync(catalog, request.Message, onlyAvailable, cancellationToken);
        if (books.Count > 0) await Emit(new ChatStreamEvent("books", Books: books));
        ChatUsage? usage = await ai.StreamAsync(request, books, text => Emit(new ChatStreamEvent("delta", Text: text)), cancellationToken);
        await Emit(new ChatStreamEvent("done", Usage: usage));
        app.Logger.LogInformation(
            "Chat completed UserHash={UserHash} ElapsedMs={ElapsedMs} InputTokens={InputTokens} OutputTokens={OutputTokens}",
            userHash, stopwatch.ElapsedMilliseconds, usage?.InputTokens, usage?.OutputTokens);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        // Client cancellation is expected; do not attempt to write to a closed response.
    }
    catch (Exception ex)
    {
        app.Logger.LogError(
            "Chat failed ErrorCode={ErrorCode} UserHash={UserHash} ElapsedMs={ElapsedMs} ExceptionType={ExceptionType}",
            "service_unavailable", userHash, stopwatch.ElapsedMilliseconds, ex.GetType().Name);
        if (!http.Response.HasStarted)
            http.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        else
            await Emit(new ChatStreamEvent("error", Text: "Dịch vụ AI đang tạm thời không phản hồi.", ErrorCode: "service_unavailable", Retryable: true));
    }

    return Results.Empty;
}).RequireAuthorization().RequireRateLimiting("chat");

app.Run();

static bool IsAllowed(System.Security.Claims.ClaimsPrincipal user, string? group)
{
    if (user.Identity?.IsAuthenticated != true) return false;
    return !string.IsNullOrWhiteSpace(group) && user.IsInRole(group);
}

static string HashIdentity(string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return "anonymous";
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..12];
}

static async Task<IReadOnlyList<BookSuggestion>> FindCatalogMatchesAsync(
    CatalogService catalog,
    string message,
    bool onlyAvailable,
    CancellationToken cancellationToken)
{
    if (CatalogQueryPlanner.TryGetBookId(message, out int maSach))
    {
        BookSuggestion? exactBook = await catalog.GetByIdAsync(maSach, cancellationToken);
        if (exactBook is not null && (!onlyAvailable || exactBook.SoLuong > 0)) return [exactBook];
    }

    var matches = new Dictionary<int, BookSuggestion>();
    foreach (string query in CatalogQueryPlanner.BuildQueries(message))
    {
        IReadOnlyList<BookSuggestion> found = await catalog.SearchAsync(query, onlyAvailable, 10 - matches.Count, cancellationToken);
        foreach (BookSuggestion book in found) matches.TryAdd(book.MaSach, book);
        if (matches.Count >= 10) break;
    }

    if (matches.Count == 0 && onlyAvailable)
    {
        foreach (BookSuggestion book in await catalog.SearchAsync(string.Empty, true, 10, cancellationToken))
            matches.TryAdd(book.MaSach, book);
    }

    return matches.Values.Take(10).ToArray();
}

static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
