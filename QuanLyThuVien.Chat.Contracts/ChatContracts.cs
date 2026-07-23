namespace QuanLyThuVien.Chat.Contracts;

public sealed record ChatRequest(
    string SessionId,
    string Message,
    IReadOnlyList<ChatHistoryItem>? History = null);

public sealed record ChatHistoryItem(string Role, string Content);

public sealed record BookSuggestion(
    int MaSach,
    string TenSach,
    string TenTacGia,
    string TenTheLoai,
    int SoLuong,
    decimal GiaTien,
    string MaISBN,
    string MoTa);

public sealed record ChatUsage(int InputTokens, int OutputTokens, int TotalTokens);

public sealed record ChatStreamEvent(
    string Type,
    string? Text = null,
    IReadOnlyList<BookSuggestion>? Books = null,
    string? ErrorCode = null,
    bool Retryable = false,
    ChatUsage? Usage = null);

public static class ChatRequestValidator
{
    public const int MaxMessageLength = 1000;
    public const int MaxHistoryItems = 8;

    public static string? Validate(ChatRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message)) return "Câu hỏi không được để trống.";
        if (request.Message.Length > MaxMessageLength) return "Câu hỏi tối đa 1.000 ký tự.";
        if (request.History?.Any(item => item.Role is not ("user" or "assistant") || string.IsNullOrWhiteSpace(item.Content)) == true)
            return "Lịch sử hội thoại không hợp lệ.";
        return null;
    }

    public static IReadOnlyList<ChatHistoryItem> LimitHistory(IEnumerable<ChatHistoryItem>? history) =>
        (history ?? Array.Empty<ChatHistoryItem>())
            .Where(item => item.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(item.Content))
            .Select(item => new ChatHistoryItem(item.Role, item.Content[..Math.Min(item.Content.Length, MaxMessageLength)]))
            .TakeLast(MaxHistoryItems)
            .ToArray();
}
