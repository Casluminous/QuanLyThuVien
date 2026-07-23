using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MdColor = MigraDoc.DocumentObjectModel.Color;
using MdOrientation = MigraDoc.DocumentObjectModel.Orientation;

namespace QuanLyThuVien.Pdf;

public abstract class PdfDocumentBase
{
    protected static (Document Document, Section Section) CreateDocument(
        string title,
        string? documentCode,
        bool landscape,
        string exportedBy)
    {
        var document = new Document();
        document.Info.Title = title;
        document.Info.Author = "QLTV - Hệ thống quản lý thư viện";
        document.Info.Subject = documentCode ?? title;

        Style normal = document.Styles["Normal"]!;
        normal.Font.Name = PdfTheme.FontFamily;
        normal.Font.Size = Unit.FromPoint(9.5);
        normal.Font.Color = PdfTheme.Text;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);

        Style heading1 = document.Styles["Heading1"]!;
        heading1.Font.Name = PdfTheme.FontFamily;
        heading1.Font.Size = Unit.FromPoint(19);
        heading1.Font.Bold = true;
        heading1.Font.Color = PdfTheme.PrimaryDark;
        heading1.ParagraphFormat.SpaceBefore = Unit.FromPoint(2);
        heading1.ParagraphFormat.SpaceAfter = Unit.FromPoint(5);
        heading1.ParagraphFormat.KeepWithNext = true;

        Style heading2 = document.Styles["Heading2"]!;
        heading2.Font.Name = PdfTheme.FontFamily;
        heading2.Font.Size = Unit.FromPoint(11.5);
        heading2.Font.Bold = true;
        heading2.Font.Color = PdfTheme.PrimaryDark;
        heading2.ParagraphFormat.SpaceBefore = Unit.FromPoint(10);
        heading2.ParagraphFormat.SpaceAfter = Unit.FromPoint(5);
        heading2.ParagraphFormat.KeepWithNext = true;

        Section section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = landscape ? MdOrientation.Landscape : MdOrientation.Portrait;
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.35);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.35);
        section.PageSetup.TopMargin = Unit.FromCentimeter(2.35);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.75);
        section.PageSetup.HeaderDistance = Unit.FromCentimeter(0.55);
        section.PageSetup.FooterDistance = Unit.FromCentimeter(0.55);
        section.PageSetup.OddAndEvenPagesHeaderFooter = false;
        section.PageSetup.DifferentFirstPageHeaderFooter = true;

        AddHeader(section, landscape);
        AddFooter(section, documentCode, exportedBy, landscape);

        Paragraph titleParagraph = section.AddParagraph(title, "Heading1");
        titleParagraph.Format.Borders.Bottom.Width = Unit.FromPoint(1.2);
        titleParagraph.Format.Borders.Bottom.Color = PdfTheme.Primary;
        titleParagraph.Format.Borders.DistanceFromBottom = Unit.FromPoint(5);

        if (!string.IsNullOrWhiteSpace(documentCode))
        {
            Paragraph codeParagraph = section.AddParagraph(documentCode);
            codeParagraph.Format.Font.Size = Unit.FromPoint(9);
            codeParagraph.Format.Font.Color = PdfTheme.SecondaryText;
            codeParagraph.Format.SpaceAfter = Unit.FromPoint(9);
        }

        return (document, section);
    }

    protected static Paragraph AddSectionTitle(Section section, string title) =>
        section.AddParagraph(title.ToUpperInvariant(), "Heading2");

    protected static Table AddInfoTable(Section section, params (string Label, string Value)[] items)
    {
        double width = section.PageSetup.Orientation == MdOrientation.Landscape ? 26.3 : 18.0;
        var table = new Table
        {
            Borders = { Width = Unit.FromPoint(0.45), Color = PdfTheme.Border },
            TopPadding = Unit.FromPoint(5),
            BottomPadding = Unit.FromPoint(5),
            LeftPadding = Unit.FromPoint(7),
            RightPadding = Unit.FromPoint(7)
        };
        table.AddColumn(Unit.FromCentimeter(width * 0.17));
        table.AddColumn(Unit.FromCentimeter(width * 0.33));
        table.AddColumn(Unit.FromCentimeter(width * 0.17));
        table.AddColumn(Unit.FromCentimeter(width * 0.33));

        for (int i = 0; i < items.Length; i += 2)
        {
            Row row = table.AddRow();
            row.Shading.Color = (i / 2) % 2 == 0 ? PdfTheme.Content : PdfTheme.Surface;
            AddInfoPair(row, 0, items[i]);
            if (i + 1 < items.Length)
                AddInfoPair(row, 2, items[i + 1]);
            else
                row.Cells[2].MergeRight = 1;
        }

        section.Add(table);
        return table;
    }

    protected static Table CreateTable(Section section, IReadOnlyList<double> widthsCm)
    {
        var table = new Table
        {
            Borders = { Width = Unit.FromPoint(0.45), Color = PdfTheme.Border },
            TopPadding = Unit.FromPoint(4.5),
            BottomPadding = Unit.FromPoint(4.5),
            LeftPadding = Unit.FromPoint(5),
            RightPadding = Unit.FromPoint(5)
        };
        foreach (double width in widthsCm)
            table.AddColumn(Unit.FromCentimeter(width));
        section.Add(table);
        return table;
    }

    protected static Row AddHeaderRow(Table table, params string[] headings)
    {
        Row row = table.AddRow();
        row.HeadingFormat = true;
        row.Shading.Color = PdfTheme.Primary;
        row.Format.Font.Bold = true;
        row.Format.Font.Color = PdfTheme.Surface;
        row.VerticalAlignment = VerticalAlignment.Center;
        for (int i = 0; i < headings.Length && i < table.Columns.Count; i++)
        {
            Paragraph paragraph = row.Cells[i].AddParagraph();
            paragraph.AddText(headings[i]);
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.Format.Font.Color = PdfTheme.Surface;
            paragraph.Format.Font.Bold = true;
        }
        return row;
    }

    protected static void AddTableCell(Row row, int index, string value, PdfCellAlignment alignment = PdfCellAlignment.Left)
    {
        Paragraph paragraph = row.Cells[index].AddParagraph(value ?? string.Empty);
        paragraph.Format.Alignment = alignment switch
        {
            PdfCellAlignment.Center => ParagraphAlignment.Center,
            PdfCellAlignment.Right => ParagraphAlignment.Right,
            _ => ParagraphAlignment.Left
        };
        row.Cells[index].VerticalAlignment = VerticalAlignment.Center;
    }

    protected static void StyleDataRow(Row row, int rowIndex)
    {
        row.Shading.Color = rowIndex % 2 == 0 ? PdfTheme.Surface : PdfTheme.Zebra;
        row.Format.Font.Size = Unit.FromPoint(9);
    }

    protected static void AddSummaryCards(Section section, params (string Label, string Value, MdColor Color)[] cards)
    {
        if (cards.Length == 0) return;
        double usableWidth = section.PageSetup.Orientation == MdOrientation.Landscape ? 26.3 : 18.0;
        var table = new Table { TopPadding = Unit.FromPoint(5), BottomPadding = Unit.FromPoint(5) };
        double gap = 0.25;
        double cardWidth = (usableWidth - gap * (cards.Length - 1)) / cards.Length;
        for (int i = 0; i < cards.Length; i++)
        {
            table.AddColumn(Unit.FromCentimeter(cardWidth));
            if (i < cards.Length - 1) table.AddColumn(Unit.FromCentimeter(gap));
        }

        Row row = table.AddRow();
        for (int i = 0; i < cards.Length; i++)
        {
            int cellIndex = i * 2;
            Cell cell = row.Cells[cellIndex];
            cell.Shading.Color = PdfTheme.Content;
            cell.Borders.Color = cards[i].Color;
            cell.Borders.Width = Unit.FromPoint(0.8);
            cell.Borders.Left.Width = Unit.FromPoint(3);
            Paragraph label = cell.AddParagraph(cards[i].Label.ToUpperInvariant());
            label.Format.Font.Size = Unit.FromPoint(8);
            label.Format.Font.Bold = true;
            label.Format.Font.Color = PdfTheme.SecondaryText;
            Paragraph value = cell.AddParagraph(cards[i].Value);
            value.Format.Font.Size = Unit.FromPoint(14);
            value.Format.Font.Bold = true;
            value.Format.Font.Color = cards[i].Color;
            value.Format.SpaceBefore = Unit.FromPoint(2);
        }
        section.Add(table);
    }

    protected static void AddSignatures(Section section, string leftTitle, string rightTitle)
    {
        Paragraph spacing = section.AddParagraph();
        spacing.Format.SpaceBefore = Unit.FromPoint(10);
        var table = new Table();
        table.AddColumn(Unit.FromCentimeter(9));
        table.AddColumn(Unit.FromCentimeter(9));
        Row row = table.AddRow();
        AddSignatureCell(row.Cells[0], leftTitle);
        AddSignatureCell(row.Cells[1], rightTitle);
        section.Add(table);
    }

    private static void AddHeader(Section section, bool landscape)
    {
        AddHeaderContent(section.Headers.Primary, landscape);
        AddHeaderContent(section.Headers.FirstPage, landscape);
    }

    private static void AddHeaderContent(HeaderFooter container, bool landscape)
    {
        double width = landscape ? 26.3 : 18.0;
        Table table = container.AddTable();
        if (landscape)
        {
            table.AddColumn(Unit.FromCentimeter(width));
            Row landscapeRow = table.AddRow();
            landscapeRow.Borders.Bottom.Width = Unit.FromPoint(1.2);
            landscapeRow.Borders.Bottom.Color = PdfTheme.Primary;
            Paragraph line = landscapeRow.Cells[0].AddParagraph();
            line.Format.Alignment = ParagraphAlignment.Right;
            FormattedText landscapeLogo = line.AddFormattedText("QLTV");
            landscapeLogo.Bold = true;
            landscapeLogo.Font.Size = Unit.FromPoint(14);
            landscapeLogo.Color = PdfTheme.PrimaryDark;
            FormattedText landscapeSystem = line.AddFormattedText("  HỆ THỐNG QUẢN LÝ THƯ VIỆN");
            landscapeSystem.Bold = true;
            landscapeSystem.Font.Size = Unit.FromPoint(8.5);
            landscapeSystem.Color = PdfTheme.SecondaryText;
            FormattedText landscapeDate = line.AddFormattedText($"  |  Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}");
            landscapeDate.Font.Size = Unit.FromPoint(8);
            landscapeDate.Color = PdfTheme.SecondaryText;
            return;
        }

        table.AddColumn(Unit.FromCentimeter(width * 0.62));
        table.AddColumn(Unit.FromCentimeter(width * 0.38));
        Row row = table.AddRow();
        row.Borders.Bottom.Width = Unit.FromPoint(1.2);
        row.Borders.Bottom.Color = PdfTheme.Primary;

        Paragraph brand = row.Cells[0].AddParagraph();
        FormattedText logo = brand.AddFormattedText("QLTV");
        logo.Bold = true;
        logo.Font.Size = Unit.FromPoint(16);
        logo.Color = PdfTheme.PrimaryDark;
        FormattedText system = brand.AddFormattedText("  HỆ THỐNG QUẢN LÝ THƯ VIỆN");
        system.Bold = true;
        system.Font.Size = Unit.FromPoint(8.5);
        system.Color = PdfTheme.SecondaryText;

        Paragraph date = row.Cells[1].AddParagraph($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}");
        date.Format.Alignment = ParagraphAlignment.Right;
        date.Format.Font.Size = Unit.FromPoint(8);
        date.Format.Font.Color = PdfTheme.SecondaryText;
    }

    private static void AddFooter(Section section, string? documentCode, string exportedBy, bool landscape)
    {
        AddFooterContent(section.Footers.Primary, documentCode, exportedBy, landscape);
        AddFooterContent(section.Footers.FirstPage, documentCode, exportedBy, landscape);
    }

    private static void AddFooterContent(HeaderFooter container, string? documentCode, string exportedBy, bool landscape)
    {
        if (landscape)
        {
            Table table = container.AddTable();
            table.AddColumn(Unit.FromCentimeter(26.3));
            Row row = table.AddRow();
            row.Borders.Top.Width = Unit.FromPoint(0.5);
            row.Borders.Top.Color = PdfTheme.Border;
            Paragraph line = row.Cells[0].AddParagraph();
            line.Format.Alignment = ParagraphAlignment.Right;
            line.Format.Font.Name = PdfTheme.FontFamily;
            line.Format.Font.Size = Unit.FromPoint(8);
            line.Format.Font.Color = PdfTheme.SecondaryText;
            line.AddText($"Xuất bởi: {exportedBy}");
            if (!string.IsNullOrWhiteSpace(documentCode)) line.AddText($"  |  {documentCode}");
            line.AddText("  |  Trang ");
            line.AddPageField();
            line.AddText("/");
            line.AddNumPagesField();
            return;
        }

        Paragraph footer = container.AddParagraph();
        footer.Format.Borders.Top.Width = Unit.FromPoint(0.5);
        footer.Format.Borders.Top.Color = PdfTheme.Border;
        footer.Format.SpaceBefore = Unit.FromPoint(4);
        footer.Format.Font.Name = PdfTheme.FontFamily;
        footer.Format.Font.Size = Unit.FromPoint(8);
        footer.Format.Font.Color = PdfTheme.SecondaryText;
        footer.Format.Alignment = landscape ? ParagraphAlignment.Right : ParagraphAlignment.Left;
        footer.AddText($"Xuất bởi: {exportedBy}");
        if (!string.IsNullOrWhiteSpace(documentCode)) footer.AddText($"  |  {documentCode}");
        footer.AddText("  |  Trang ");
        footer.AddPageField();
        footer.AddText("/");
        footer.AddNumPagesField();
    }

    private static void AddInfoPair(Row row, int startIndex, (string Label, string Value) item)
    {
        Paragraph label = row.Cells[startIndex].AddParagraph(item.Label.ToUpperInvariant());
        label.Format.Font.Size = Unit.FromPoint(7.7);
        label.Format.Font.Bold = true;
        label.Format.Font.Color = PdfTheme.SecondaryText;
        Paragraph value = row.Cells[startIndex + 1].AddParagraph(item.Value);
        value.Format.Font.Bold = true;
        value.Format.Font.Color = PdfTheme.Text;
    }

    private static void AddSignatureCell(Cell cell, string title)
    {
        Paragraph titleParagraph = cell.AddParagraph(title.ToUpperInvariant());
        titleParagraph.Format.Alignment = ParagraphAlignment.Center;
        titleParagraph.Format.Font.Bold = true;
        titleParagraph.Format.Font.Size = Unit.FromPoint(9);
        Paragraph note = cell.AddParagraph("(Ký và ghi rõ họ tên)");
        note.Format.Alignment = ParagraphAlignment.Center;
        note.Format.Font.Italic = true;
        note.Format.Font.Size = Unit.FromPoint(8);
        note.Format.Font.Color = PdfTheme.SecondaryText;
        note.Format.SpaceAfter = Unit.FromCentimeter(1.7);
    }
}
