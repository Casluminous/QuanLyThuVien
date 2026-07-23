using System.Runtime.CompilerServices;
using System.Text;
using OpenAI.Responses;
using QuanLyThuVien.Chat.Contracts;

namespace QuanLyThuVien.ChatApi;

public sealed class OpenAiGateway
{
    private const string SystemGuardrail = "Bạn là Trợ lý thư viện QLTV. Chỉ trả lời dựa trên dữ liệu danh mục sách được cung cấp. Nếu không có dữ liệu phù hợp, nói rõ là không tìm thấy; không bịa tên sách, số lượng hoặc giá. Không thực hiện thao tác ghi dữ liệu. Trả lời tiếng Việt, ngắn gọn, thân thiện.";
    private readonly IConfiguration _configuration;
    private readonly ResponsesClient? _client;

    public OpenAiGateway(IConfiguration configuration)
    {
        _configuration = configuration;
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey)) _client = new ResponsesClient(apiKey);
    }

    public bool IsConfigured => _client != null;

    public async Task<ChatUsage?> StreamAsync(
        ChatRequest request,
        IReadOnlyList<BookSuggestion> books,
        Func<string, Task> onDelta,
        CancellationToken cancellationToken)
    {
        if (_client == null)
            throw new InvalidOperationException("OPENAI_API_KEY chưa được cấu hình trên máy chủ.");

        string model = _configuration["Chat:Model"] ?? "gpt-5.6-terra";
        var options = new CreateResponseOptions
        {
            Model = model,
            StreamingEnabled = true,
            StoredOutputEnabled = false,
            MaxOutputTokenCount = 700,
            ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.Low
            }
        };

        options.InputItems.Add(ResponseItem.CreateUserMessageItem(BuildPrompt(request, books)));

        ChatUsage? usage = null;
        await foreach (StreamingResponseUpdate update in _client
            .CreateResponseStreamingAsync(options)
            .WithCancellation(cancellationToken))
        {
            if (update is StreamingResponseOutputTextDeltaUpdate delta && !string.IsNullOrEmpty(delta.Delta))
                await onDelta(delta.Delta);
            else if (update is StreamingResponseCompletedUpdate completed)
            {
                ResponseTokenUsage tokenUsage = completed.Response.Usage;
                usage = new ChatUsage(tokenUsage.InputTokenCount, tokenUsage.OutputTokenCount, tokenUsage.TotalTokenCount);
            }
        }

        return usage;
    }

    private static string BuildPrompt(ChatRequest request, IReadOnlyList<BookSuggestion> books)
    {
        var builder = new StringBuilder();
        builder.AppendLine(SystemGuardrail);
        builder.AppendLine();
        builder.AppendLine("Lịch sử phiên hiện tại (chỉ dùng để hiểu ngữ cảnh, không lưu):");
        foreach (var item in (request.History ?? Array.Empty<ChatHistoryItem>()).Take(8))
        {
            string role = item.Role is "assistant" ? "Trợ lý" : "Người dùng";
            builder.Append(role).Append(": ").AppendLine(item.Content[..Math.Min(item.Content.Length, 1000)]);
        }

        builder.AppendLine();
        builder.AppendLine("Dữ liệu sách tìm được từ SQL Server (nguồn sự thật):");
        if (books.Count == 0)
            builder.AppendLine("Không có sách phù hợp.");
        else
            foreach (var book in books)
                builder.AppendLine($"- MaSach={book.MaSach}; {book.TenSach}; tác giả={book.TenTacGia}; thể loại={book.TenTheLoai}; số lượng={book.SoLuong}; giá={book.GiaTien:N0}đ; ISBN={book.MaISBN}; mô tả={book.MoTa}");

        builder.AppendLine();
        builder.Append("Câu hỏi mới: ").Append(request.Message.Trim());
        return builder.ToString();
    }
}
