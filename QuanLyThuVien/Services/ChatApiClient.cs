using System.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using QuanLyThuVien.Chat.Contracts;

namespace QuanLyThuVien.Services;

public sealed class ChatApiException : Exception
{
    public HttpStatusCode? StatusCode { get; }
    public bool Retryable { get; }

    public ChatApiException(string message, HttpStatusCode? statusCode = null, bool retryable = true, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        Retryable = retryable;
    }
}

public sealed class ChatApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public ChatApiClient()
    {
        var handler = new HttpClientHandler { UseDefaultCredentials = true };
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(ReadBaseUrl(), UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(65)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
    }

    public async Task StreamAsync(
        ChatRequest request,
        Func<string, Task> onDelta,
        Func<IReadOnlyList<BookSuggestion>, Task> onBooks,
        Func<Task> onDone,
        Func<ChatStreamEvent, Task> onError,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/chat/stream")
        {
            Content = new StringContent(JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ChatApiException("Kết nối trợ lý đã hết thời gian chờ.", retryable: true);
        }
        catch (HttpRequestException ex)
        {
            throw new ChatApiException("Không kết nối được máy chủ trợ lý. Kiểm tra mạng hoặc địa chỉ ChatApi.", inner: ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                string? serverMessage = TryReadServerMessage(body);
                string messageText = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Phiên Windows chưa được xác thực với máy chủ trợ lý.",
                    HttpStatusCode.Forbidden => "Tài khoản của bạn chưa được cấp quyền dùng trợ lý.",
                    (HttpStatusCode)429 => "Bạn đang gửi quá nhiều câu hỏi. Vui lòng thử lại sau ít phút.",
                    _ => serverMessage ?? "Máy chủ trợ lý đang gặp lỗi. Vui lòng thử lại."
                };
                throw new ChatApiException(messageText, response.StatusCode, response.StatusCode != HttpStatusCode.Forbidden);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string? eventName = null;
            var data = new StringBuilder();
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
                {
                    eventName = line[6..].Trim();
                    continue;
                }
                if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    if (data.Length > 0) data.Append('\n');
                    data.Append(line[5..].TrimStart());
                    continue;
                }
                if (line.Length != 0 || data.Length == 0) continue;

                var parsed = JsonSerializer.Deserialize<ChatStreamEvent>(data.ToString(), JsonOptions);
                data.Clear();
                string type = parsed?.Type ?? eventName ?? string.Empty;
                if (parsed == null) continue;
                switch (type)
                {
                    case "delta" when !string.IsNullOrEmpty(parsed.Text): await onDelta(parsed.Text); break;
                    case "books" when parsed.Books != null: await onBooks(parsed.Books); break;
                    case "done": await onDone(); break;
                    case "error": await onError(parsed); break;
                }
                eventName = null;
            }
        }
    }

    private static string ReadBaseUrl()
    {
        string configured = ConfigurationManager.AppSettings["ChatApiBaseUrl"] ?? "https://localhost:7040";
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
            throw new InvalidOperationException("ChatApiBaseUrl không hợp lệ.");
        return uri.ToString().TrimEnd('/') + "/";
    }

    private static string? TryReadServerMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("message", out JsonElement message)
                ? message.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
