using System.Data;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace QuanLyThuVien.Pdf;

public sealed class PdfDocumentFactory : PdfDocumentBase
{
    public static Document CreateGrid(PdfTableSnapshot snapshot)
    {
        bool landscape = snapshot.Columns.Count > 6;
        (Document document, Section section) = CreateDocument(snapshot.Title, null, landscape, snapshot.ExportedBy);

        if (!string.IsNullOrWhiteSpace(snapshot.Subtitle))
        {
            Paragraph subtitle = section.AddParagraph(snapshot.Subtitle);
            subtitle.Format.Font.Color = PdfTheme.SecondaryText;
            subtitle.Format.Font.Size = Unit.FromPoint(9);
            subtitle.Format.SpaceAfter = Unit.FromPoint(6);
        }

        double usableWidth = landscape ? 26.3 : 18.0;
        double[] widths = CalculateColumnWidths(snapshot, usableWidth);
        Table table = CreateTable(section, widths);
        AddHeaderRow(table, snapshot.Columns.Select(x => x.Header).ToArray());

        for (int r = 0; r < snapshot.Rows.Count; r++)
        {
            Row row = table.AddRow();
            StyleDataRow(row, r);
            IReadOnlyList<string> values = snapshot.Rows[r];
            for (int c = 0; c < snapshot.Columns.Count; c++)
            {
                string value = c < values.Count ? values[c] : string.Empty;
                AddTableCell(row, c, value, snapshot.Columns[c].Alignment);
            }
        }
        return document;
    }

    public static Document CreateLoanReceipt(DataRow loan, DataTable details, string exportedBy)
    {
        int maPM = Convert.ToInt32(loan["MaPhieuMuon"]);
        string status = loan["TrangThai"]?.ToString() ?? string.Empty;
        (Document document, Section section) = CreateDocument(
            $"Phiếu mượn #{maPM}",
            $"Mã phiếu: PM-{maPM:000000}",
            false,
            exportedBy);

        AddInfoTable(section,
            ("Độc giả", Value(loan, "TenDocGia")),
            ("Nhân viên lập phiếu", Value(loan, "TenNhanVien")),
            ("Ngày mượn", DateValue(loan, "NgayMuon")),
            ("Hạn trả", DateValue(loan, "HanTra")),
            ("Trạng thái", status),
            ("Số đầu sách", details.Rows.Count.ToString("N0")));

        AddSectionTitle(section, "Danh sách sách");
        Table table = CreateTable(section, new[] { 0.9, 7.0, 1.4, 3.1, 3.1, 2.5 });
        AddHeaderRow(table, "STT", "Tên sách", "Số lượng", "Giá sách", "Kết quả", "Thành tiền");
        decimal totalValue = 0;
        for (int i = 0; i < details.Rows.Count; i++)
        {
            DataRow detail = details.Rows[i];
            int quantity = Convert.ToInt32(detail["SoLuong"]);
            decimal price = Convert.ToDecimal(detail["GiaTien"]);
            bool returned = detail["NgayTra"] != DBNull.Value;
            int lost = detail.Table.Columns.Contains("SoLuongMat") && detail["SoLuongMat"] != DBNull.Value ? Convert.ToInt32(detail["SoLuongMat"]) : 0;
            string result = !returned ? "Chưa trả" : lost <= 0 ? "Đã trả" : $"Trả {quantity - lost} / Mất {lost}";
            totalValue += price * quantity;

            Row row = table.AddRow();
            StyleDataRow(row, i);
            AddTableCell(row, 0, (i + 1).ToString(), PdfCellAlignment.Center);
            AddTableCell(row, 1, Value(detail, "TenSach"));
            AddTableCell(row, 2, quantity.ToString("N0"), PdfCellAlignment.Center);
            AddTableCell(row, 3, Money(price), PdfCellAlignment.Right);
            AddTableCell(row, 4, result, PdfCellAlignment.Center);
            AddTableCell(row, 5, Money(price * quantity), PdfCellAlignment.Right);
        }

        AddSummaryCards(section,
            ("Tổng số lượng", details.Rows.Cast<DataRow>().Sum(row => Convert.ToInt32(row["SoLuong"])).ToString("N0"), PdfTheme.Primary),
            ("Giá trị tham khảo", Money(totalValue), PdfTheme.Accent),
            ("Trạng thái", status, status.Contains("trả", StringComparison.OrdinalIgnoreCase) ? PdfTheme.Success : PdfTheme.Primary));

        Paragraph note = section.AddParagraph("Trách nhiệm: độc giả bảo quản sách và hoàn trả đúng hạn theo quy định của thư viện.");
        note.Format.Font.Color = PdfTheme.SecondaryText;
        note.Format.Font.Italic = true;
        note.Format.SpaceBefore = Unit.FromPoint(7);
        AddSignatures(section, "Độc giả", "Nhân viên thư viện");
        return document;
    }

    public static Document CreateReturnInvoice(DataRow loan, DataTable details, DataTable summary, string exportedBy)
    {
        int maPM = Convert.ToInt32(loan["MaPhieuMuon"]);
        (Document document, Section section) = CreateDocument(
            $"Hóa đơn trả sách #{maPM}",
            $"Mã phiếu: PM-{maPM:000000}  |  Ngày lập: {DateTime.Now:dd/MM/yyyy}",
            false,
            exportedBy);

        AddInfoTable(section,
            ("Độc giả", Value(loan, "TenDocGia")),
            ("Nhân viên lập phiếu", Value(loan, "TenNhanVien")),
            ("Ngày mượn", DateValue(loan, "NgayMuon")),
            ("Hạn trả", DateValue(loan, "HanTra")),
            ("Ngày xuất hóa đơn", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
            ("Trạng thái phiếu", Value(loan, "TrangThai")));

        AddSectionTitle(section, "Chi tiết hoàn trả và khoản phải thu");
        Table table = CreateTable(section, new[] { 0.7, 5.7, 1.15, 2.0, 2.6, 2.7, 2.7 });
        AddHeaderRow(table, "STT", "Tên sách", "SL", "Kết quả", "Phạt quá hạn", "Đền mất sách", "Thành tiền");
        decimal total = 0;
        int rowIndex = 0;
        foreach (DataRow detail in details.Rows)
        {
            if (detail["NgayTra"] == DBNull.Value) continue;
            int quantity = Convert.ToInt32(detail["SoLuong"]);
            int lost = detail.Table.Columns.Contains("SoLuongMat") && detail["SoLuongMat"] != DBNull.Value ? Convert.ToInt32(detail["SoLuongMat"]) : 0;
            decimal fine = Convert.ToDecimal(detail["TienPhat"]);
            decimal compensation = Convert.ToDecimal(detail["TienDenMatSach"]);
            decimal lineTotal = fine + compensation;
            total += lineTotal;
            Row row = table.AddRow();
            StyleDataRow(row, rowIndex);
            AddTableCell(row, 0, (++rowIndex).ToString(), PdfCellAlignment.Center);
            AddTableCell(row, 1, Value(detail, "TenSach"));
            AddTableCell(row, 2, quantity.ToString("N0"), PdfCellAlignment.Center);
            AddTableCell(row, 3, lost == 0 ? "Đã trả" : lost >= quantity ? "Mất toàn bộ" : $"Mất {lost}", PdfCellAlignment.Center);
            AddTableCell(row, 4, Money(fine), PdfCellAlignment.Right);
            AddTableCell(row, 5, Money(compensation), PdfCellAlignment.Right);
            AddTableCell(row, 6, Money(lineTotal), PdfCellAlignment.Right);
        }

        if (rowIndex == 0)
        {
            Row empty = table.AddRow();
            empty.Shading.Color = PdfTheme.Content;
            empty.Cells[0].MergeRight = 6;
            AddTableCell(empty, 0, "Chưa có sách đã trả.", PdfCellAlignment.Center);
        }

        decimal due = summary.Rows.Count == 0 ? total : Convert.ToDecimal(summary.Rows[0]["TongPhaiThu"]);
        decimal paid = summary.Rows.Count == 0 ? 0 : Convert.ToDecimal(summary.Rows[0]["DaThu"]);
        decimal remaining = Math.Max(0, due - paid);
        string paymentStatus = remaining <= 0 ? "Đã thanh toán đủ" : paid > 0 ? "Đã thanh toán một phần" : "Chưa thanh toán";
        AddSummaryCards(section,
            ("Tổng phải thu", Money(due), PdfTheme.Warning),
            ("Đã thu", Money(paid), PdfTheme.Success),
            ("Còn lại", Money(remaining), remaining > 0 ? PdfTheme.Danger : PdfTheme.Success),
            ("Thanh toán", paymentStatus, remaining > 0 ? PdfTheme.Warning : PdfTheme.Success));

        Paragraph note = section.AddParagraph("Khoản đền mất sách được tính theo giá niêm yết của từng cuốn tại thời điểm trả.");
        note.Format.Font.Color = PdfTheme.SecondaryText;
        note.Format.Font.Italic = true;
        note.Format.SpaceBefore = Unit.FromPoint(7);
        AddSignatures(section, "Độc giả", "Nhân viên thu tiền");
        return document;
    }

    public static Document CreateReaderHistory(DataRow reader, DataTable history, string exportedBy)
    {
        int maDG = Convert.ToInt32(reader["MaDG"]);
        (Document document, Section section) = CreateDocument(
            $"Lịch sử độc giả - {Value(reader, "HoTen")}",
            $"Mã độc giả: DG-{maDG:000000}",
            true,
            exportedBy);

        AddInfoTable(section,
            ("Họ tên", Value(reader, "HoTen")),
            ("Số điện thoại", Value(reader, "SoDienThoai")),
            ("Email", Value(reader, "Email")),
            ("Ngày lập thẻ", DateValue(reader, "NgayLapThe")),
            ("Hạn sử dụng", DateValue(reader, "HanSuDung")),
            ("Trạng thái", Convert.ToBoolean(reader["TrangThai"]) ? "Đang hoạt động" : "Tạm khóa"));

        int loanCount = history.Rows.Count;
        decimal fineTotal = history.Rows.Cast<DataRow>().Sum(row => Convert.ToDecimal(row["TongTienPhat"]));
        decimal compensationTotal = history.Rows.Cast<DataRow>().Sum(row => Convert.ToDecimal(row["TongTienDen"]));
        AddSummaryCards(section,
            ("Số phiếu mượn", loanCount.ToString("N0"), PdfTheme.Primary),
            ("Tổng phạt", Money(fineTotal), PdfTheme.Warning),
            ("Tổng đền mất", Money(compensationTotal), PdfTheme.Danger));

        AddSectionTitle(section, "Lịch sử mượn trả");
        Table table = CreateTable(section, new[] { 1.6, 4.0, 4.0, 3.2, 3.0, 3.3, 3.5, 3.7 });
        AddHeaderRow(table, "Mã PM", "Ngày mượn", "Hạn trả", "Ngày trả cuối", "Số đầu sách", "Trạng thái", "Tiền phạt", "Đền mất sách");
        for (int i = 0; i < history.Rows.Count; i++)
        {
            DataRow item = history.Rows[i];
            Row row = table.AddRow();
            StyleDataRow(row, i);
            AddTableCell(row, 0, $"PM-{Convert.ToInt32(item["MaPhieuMuon"]):000000}", PdfCellAlignment.Center);
            AddTableCell(row, 1, DateValue(item, "NgayMuon"), PdfCellAlignment.Center);
            AddTableCell(row, 2, DateValue(item, "HanTra"), PdfCellAlignment.Center);
            AddTableCell(row, 3, DateValue(item, "NgayTraCuoi"), PdfCellAlignment.Center);
            AddTableCell(row, 4, Convert.ToInt32(item["SoDauSach"]).ToString("N0"), PdfCellAlignment.Center);
            AddTableCell(row, 5, Value(item, "TrangThai"), PdfCellAlignment.Center);
            AddTableCell(row, 6, Money(Convert.ToDecimal(item["TongTienPhat"])), PdfCellAlignment.Right);
            AddTableCell(row, 7, Money(Convert.ToDecimal(item["TongTienDen"])), PdfCellAlignment.Right);
        }
        if (history.Rows.Count == 0)
        {
            Row row = table.AddRow();
            row.Cells[0].MergeRight = 7;
            AddTableCell(row, 0, "Độc giả chưa có lịch sử mượn sách.", PdfCellAlignment.Center);
        }

        return document;
    }

    private static double[] CalculateColumnWidths(PdfTableSnapshot snapshot, double totalWidth)
    {
        var weights = snapshot.Columns.Select((column, index) =>
        {
            int maxLength = Math.Max(column.Header.Length, snapshot.Rows.Select(row => index < row.Count ? row[index]?.Length ?? 0 : 0).DefaultIfEmpty().Max());
            return Math.Clamp(maxLength, 6, 28) + (column.Alignment == PdfCellAlignment.Right ? 2 : 0);
        }).ToArray();
        double sum = weights.Sum();
        double[] widths = weights.Select(weight => Math.Max(1.1, totalWidth * weight / sum)).ToArray();
        double scale = totalWidth / widths.Sum();
        return widths.Select(width => width * scale).ToArray();
    }

    private static string Value(DataRow row, string column) => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? row[column]?.ToString() ?? string.Empty : string.Empty;

    private static string DateValue(DataRow row, string column) => row.Table.Columns.Contains(column) && row[column] != DBNull.Value && DateTime.TryParse(row[column]?.ToString(), out DateTime date) ? date.ToString("dd/MM/yyyy") : "-";

    private static string Money(decimal value) => $"{value:N0} đ";
}
