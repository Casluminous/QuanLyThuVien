using QuanLyThuVien.Chat.Contracts;
using Xunit;

namespace QuanLyThuVien.ChatApi.Tests;

public sealed class ChatRequestValidatorTests
{
    [Fact]
    public void RejectsEmptyAndOversizedMessage()
    {
        Assert.NotNull(ChatRequestValidator.Validate(new ChatRequest("s", "   ")));
        Assert.NotNull(ChatRequestValidator.Validate(new ChatRequest("s", new string('x', 1001))));
    }

    [Fact]
    public void LimitsHistoryToEightValidItems()
    {
        var history = Enumerable.Range(0, 12).Select(i => new ChatHistoryItem("user", $"q{i}"));
        var result = ChatRequestValidator.LimitHistory(history);
        Assert.Equal(8, result.Count);
        Assert.Equal("q4", result[0].Content);
        Assert.Equal("q11", result[^1].Content);
    }

    [Fact]
    public void RejectsUnknownHistoryRole()
    {
        var error = ChatRequestValidator.Validate(new ChatRequest("s", "hello", [new ChatHistoryItem("system", "bad")]));
        Assert.Equal("Lịch sử hội thoại không hợp lệ.", error);
    }
}
