namespace QuanLyThuVien.Pdf;

public enum PdfCellAlignment
{
    Left,
    Center,
    Right
}

public sealed record PdfTableColumn(string Header, PdfCellAlignment Alignment = PdfCellAlignment.Left);

public sealed record PdfTableSnapshot(
    string Title,
    string? Subtitle,
    IReadOnlyList<PdfTableColumn> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    string ExportedBy);
