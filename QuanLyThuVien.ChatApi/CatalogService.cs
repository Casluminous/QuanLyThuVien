using System.Data;
using System.Data.SqlClient;
using QuanLyThuVien.Chat.Contracts;

namespace QuanLyThuVien.ChatApi;

public sealed class CatalogService
{
    private readonly string _connectionString;

    public CatalogService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("QuanLyThuVien") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_connectionString))
            _connectionString = Environment.GetEnvironmentVariable("QLTV_CHAT_DB_CONNECTION") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_connectionString))
            _connectionString = "Server=.\\SQLEXPRESS;Database=QuanLyThuVien;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand("SELECT 1", connection);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<BookSuggestion>> SearchAsync(string query, bool onlyAvailable, int limit, CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 10);
        query = (query ?? string.Empty).Trim();
        const string sql = @"
SELECT TOP (@limit)
    s.MaSach, s.TenSach, COALESCE(tg.TenTG, N'') AS TenTacGia,
    COALESCE(tl.TenTheLoai, N'') AS TenTheLoai, s.SoLuong, s.GiaTien,
    COALESCE(s.MaISBN, N'') AS MaISBN, COALESCE(s.MoTa, N'') AS MoTa
FROM Sach s
LEFT JOIN TacGia tg ON tg.MaTG = s.MaTG
LEFT JOIN TheLoai tl ON tl.MaTL = s.MaTL
LEFT JOIN NhaXuatBan nxb ON nxb.MaNXB = s.MaNXB
WHERE (@query = N'' OR s.TenSach LIKE @like OR s.MaISBN LIKE @like
       OR tg.TenTG LIKE @like OR tl.TenTheLoai LIKE @like
       OR nxb.TenNXB LIKE @like OR s.MoTa LIKE @like)
  AND (@onlyAvailable = 0 OR s.SoLuong > 0)
ORDER BY CASE WHEN @query <> N'' AND s.TenSach LIKE @exact THEN 0 ELSE 1 END,
         s.TenSach ASC";

        var rows = new List<BookSuggestion>(limit);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@limit", SqlDbType.Int) { Value = limit });
        command.Parameters.Add(new SqlParameter("@query", SqlDbType.NVarChar, 400) { Value = query });
        command.Parameters.Add(new SqlParameter("@like", SqlDbType.NVarChar, 410) { Value = $"%{EscapeLike(query)}%" });
        command.Parameters.Add(new SqlParameter("@exact", SqlDbType.NVarChar, 410) { Value = $"%{EscapeLike(query)}%" });
        command.Parameters.Add(new SqlParameter("@onlyAvailable", SqlDbType.Bit) { Value = onlyAvailable });

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new BookSuggestion(
                reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetInt32(4), reader.GetDecimal(5), reader.GetString(6), reader.GetString(7)));
        }

        return rows;
    }

    public async Task<BookSuggestion?> GetByIdAsync(int maSach, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT TOP (1) s.MaSach, s.TenSach, COALESCE(tg.TenTG, N''), COALESCE(tl.TenTheLoai, N''),
       s.SoLuong, s.GiaTien, COALESCE(s.MaISBN, N''), COALESCE(s.MoTa, N'')
FROM Sach s
LEFT JOIN TacGia tg ON tg.MaTG = s.MaTG
LEFT JOIN TheLoai tl ON tl.MaTL = s.MaTL
WHERE s.MaSach = @maSach";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@maSach", SqlDbType.Int) { Value = maSach });
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new BookSuggestion(
            reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetInt32(4), reader.GetDecimal(5), reader.GetString(6), reader.GetString(7));
    }

    private static string EscapeLike(string value) => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
