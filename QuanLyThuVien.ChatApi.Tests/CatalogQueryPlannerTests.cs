using Xunit;

namespace QuanLyThuVien.ChatApi.Tests;

public sealed class CatalogQueryPlannerTests
{
    [Theory]
    [InlineData("Gợi ý truyện tranh còn trong kho", "truyện tranh")]
    [InlineData("Tìm sách của Nguyễn Nhật Ánh", "Nguyễn Nhật Ánh")]
    public void BuildsUsefulVietnameseCatalogQuery(string message, string expected)
    {
        IReadOnlyList<string> queries = CatalogQueryPlanner.BuildQueries(message);
        Assert.Contains(expected, queries, StringComparer.OrdinalIgnoreCase);
        Assert.True(queries.Count <= 6);
    }

    [Theory]
    [InlineData("Mở mã sách 12", 12)]
    [InlineData("Chi tiết MaSach: 7", 7)]
    public void ExtractsExplicitBookId(string message, int expected)
    {
        Assert.True(CatalogQueryPlanner.TryGetBookId(message, out int actual));
        Assert.Equal(expected, actual);
    }
}
