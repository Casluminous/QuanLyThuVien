using MdColor = MigraDoc.DocumentObjectModel.Color;

namespace QuanLyThuVien.Pdf;

public static class PdfTheme
{
    public const string FontFamily = "Segoe UI";

    public static readonly MdColor Primary = MdColor.FromRgb(15, 118, 110);
    public static readonly MdColor PrimaryDark = MdColor.FromRgb(17, 94, 89);
    public static readonly MdColor PrimaryLight = MdColor.FromRgb(204, 251, 241);
    public static readonly MdColor Accent = MdColor.FromRgb(217, 119, 6);
    public static readonly MdColor Success = MdColor.FromRgb(21, 128, 61);
    public static readonly MdColor Warning = MdColor.FromRgb(180, 83, 9);
    public static readonly MdColor Danger = MdColor.FromRgb(220, 38, 38);
    public static readonly MdColor Surface = MdColor.FromRgb(255, 255, 255);
    public static readonly MdColor Content = MdColor.FromRgb(246, 250, 249);
    public static readonly MdColor Border = MdColor.FromRgb(216, 229, 226);
    public static readonly MdColor Text = MdColor.FromRgb(23, 48, 45);
    public static readonly MdColor SecondaryText = MdColor.FromRgb(91, 112, 108);
    public static readonly MdColor Zebra = MdColor.FromRgb(240, 248, 246);
}
