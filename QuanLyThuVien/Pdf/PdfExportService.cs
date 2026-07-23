using System.Data;
using System.Text.RegularExpressions;
using MigraDoc.Rendering;
using QuanLyThuVien.Data;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Pdf;

public static class PdfExportService
{
    public static bool ExportGrid(DataGridView grid, string title, string defaultName, IWin32Window owner, string? subtitle = null)
    {
        PdfTableSnapshot? snapshot = CreateSnapshot(grid, title, subtitle);
        if (snapshot == null)
        {
            MessageBox.Show(owner, "Không có dữ liệu để xuất PDF.", "Xuất PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        return SaveDocument(PdfDocumentFactory.CreateGrid(snapshot), defaultName, owner);
    }

    public static bool ExportLoanReceipt(int maPM, IWin32Window owner)
    {
        try
        {
            DataTable loan = DataAccess.GetPhieuMuonById(maPM);
            if (loan.Rows.Count == 0) return ShowNotFound(owner, "Không tìm thấy phiếu mượn.");
            DataTable details = DataAccess.GetChiTietPhieuMuon(maPM);
            if (details.Rows.Count == 0) return ShowNotFound(owner, "Phiếu mượn chưa có chi tiết sách.");
            return SaveDocument(PdfDocumentFactory.CreateLoanReceipt(loan.Rows[0], details, ExportedBy()), $"phieu_muon_{maPM}", owner);
        }
        catch (Exception ex)
        {
            return ShowError(owner, "Không thể tạo PDF phiếu mượn.", ex);
        }
    }

    public static bool ExportReturnInvoice(int maPM, IWin32Window owner)
    {
        try
        {
            DataTable loan = DataAccess.GetPhieuMuonById(maPM);
            if (loan.Rows.Count == 0) return ShowNotFound(owner, "Không tìm thấy phiếu mượn.");
            DataTable details = DataAccess.GetChiTietPhieuMuon(maPM);
            if (!details.Rows.Cast<DataRow>().Any(row => row["NgayTra"] != DBNull.Value))
                return ShowNotFound(owner, "Phiếu này chưa có sách đã trả nên chưa thể xuất hóa đơn.");
            DataTable summary = DataAccess.GetLoanPaymentSummary(maPM);
            return SaveDocument(PdfDocumentFactory.CreateReturnInvoice(loan.Rows[0], details, summary, ExportedBy()), $"hoa_don_tra_{maPM}", owner);
        }
        catch (Exception ex)
        {
            return ShowError(owner, "Không thể tạo PDF hóa đơn trả sách.", ex);
        }
    }

    public static bool ExportReaderHistory(int maDG, IWin32Window owner)
    {
        try
        {
            DataTable reader = DataAccess.GetDocGiaById(maDG);
            if (reader.Rows.Count == 0) return ShowNotFound(owner, "Không tìm thấy độc giả.");
            DataTable history = DataAccess.GetDocGiaHistory(maDG);
            return SaveDocument(PdfDocumentFactory.CreateReaderHistory(reader.Rows[0], history, ExportedBy()), $"lich_su_doc_gia_{maDG}", owner);
        }
        catch (Exception ex)
        {
            return ShowError(owner, "Không thể tạo PDF lịch sử độc giả.", ex);
        }
    }

    private static PdfTableSnapshot? CreateSnapshot(DataGridView grid, string title, string? subtitle)
    {
        var sourceColumns = grid.Columns.Cast<DataGridViewColumn>()
            .Where(column => column.Visible && !IsActionColumn(column))
            .ToList();
        if (sourceColumns.Count == 0) return null;

        var columns = sourceColumns
            .Select(column => new PdfTableColumn(column.HeaderText, DetectAlignment(column)))
            .ToList();
        var rows = new List<IReadOnlyList<string>>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow || !row.Visible) continue;
            rows.Add(sourceColumns.Select(column => FormatCell(row.Cells[column.Index].FormattedValue)).ToArray());
        }
        return rows.Count == 0 ? null : new PdfTableSnapshot(title, subtitle, columns, rows, ExportedBy());
    }

    private static bool SaveDocument(MigraDoc.DocumentObjectModel.Document document, string defaultName, IWin32Window owner)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Tệp PDF (*.pdf)|*.pdf",
            DefaultExt = "pdf",
            AddExtension = true,
            FileName = $"{SanitizeFileName(defaultName)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            Title = "Lưu PDF"
        };
        if (dialog.ShowDialog(owner) != DialogResult.OK) return false;
        try
        {
            var renderer = new PdfDocumentRenderer { Document = document, Language = "vi-VN" };
            renderer.RenderDocument();
            renderer.Save(dialog.FileName);
            MessageBox.Show(owner, $"Đã lưu PDF:\n{dialog.FileName}", "Xuất PDF thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }
        catch (Exception ex)
        {
            return ShowError(owner, "Không thể lưu file PDF. Hãy thử một thư mục khác.", ex);
        }
    }

    private static PdfCellAlignment DetectAlignment(DataGridViewColumn column)
    {
        string header = column.HeaderText ?? string.Empty;
        if (header.Contains("tiền", StringComparison.OrdinalIgnoreCase)
            || header.Contains("giá", StringComparison.OrdinalIgnoreCase)
            || header.Contains("phạt", StringComparison.OrdinalIgnoreCase)
            || header.Contains("doanh thu", StringComparison.OrdinalIgnoreCase))
            return PdfCellAlignment.Right;
        if (header.Contains("ngày", StringComparison.OrdinalIgnoreCase)
            || header.Contains("hạn", StringComparison.OrdinalIgnoreCase)
            || header.Contains("mã", StringComparison.OrdinalIgnoreCase)
            || header.Contains("số", StringComparison.OrdinalIgnoreCase)
            || header.Equals("SL", StringComparison.OrdinalIgnoreCase))
            return PdfCellAlignment.Center;
        return PdfCellAlignment.Left;
    }

    private static bool IsActionColumn(DataGridViewColumn column)
    {
        if (column is DataGridViewButtonColumn || !column.Visible) return true;
        string name = (column.Name + " " + column.HeaderText).Trim();
        return name.Contains("btn", StringComparison.OrdinalIgnoreCase)
            || name.Contains("thao tác", StringComparison.OrdinalIgnoreCase)
            || name.Contains("chi tiết", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Sửa", StringComparison.OrdinalIgnoreCase)
            || name.Contains("gia hạn", StringComparison.OrdinalIgnoreCase)
            || name.Contains("thu tiền", StringComparison.OrdinalIgnoreCase)
            || name.Contains("tác vụ", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatCell(object? value)
    {
        if (value == null || value == DBNull.Value) return "-";
        if (value is DateTime date) return date.ToString("dd/MM/yyyy");
        return value.ToString() ?? string.Empty;
    }

    private static string ExportedBy() => Session.CurrentUser?.HoTen ?? "Người dùng";

    private static string SanitizeFileName(string value) => Regex.Replace(value, @"[^a-zA-Z0-9_\-]+", "_").Trim('_');

    private static bool ShowNotFound(IWin32Window owner, string message)
    {
        MessageBox.Show(owner, message, "Xuất PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    private static bool ShowError(IWin32Window owner, string message, Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"PDF export failed: {ex}");
        MessageBox.Show(owner, message, "Lỗi xuất PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }
}
