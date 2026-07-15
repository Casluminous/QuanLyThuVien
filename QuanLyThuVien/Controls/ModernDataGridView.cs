using System.Reflection;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls
{
    public class ModernDataGridView : DataGridView
    {
        private readonly Font _boldFont;

        public ModernDataGridView()
        {
            _boldFont = new Font(DefaultCellStyle.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
            // Set styles for flat modern look
            BackgroundColor = Color.White;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            GridColor = Color.FromArgb(241, 245, 249); // slate-100

            // Configuration
            RowHeadersVisible = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            ReadOnly = true;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            EnableHeadersVisualStyles = false;

            // Row heights
            RowTemplate.Height = 40;
            ColumnHeadersHeight = 44;

            // Default Cell Style (slate-700 text)
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85), // slate-700
                BackColor = Color.White,
                SelectionBackColor = Color.FromArgb(238, 242, 255), // indigo-50
                SelectionForeColor = AppColors.Primary,
                Padding = new Padding(8, 0, 8, 0)
            };

            // Alternating Row Style (slate-50 background)
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85),
                BackColor = Color.FromArgb(248, 250, 252), // slate-50
                SelectionBackColor = Color.FromArgb(238, 242, 255),
                SelectionForeColor = AppColors.Primary,
                Padding = new Padding(8, 0, 8, 0)
            };

            // Column Header Style
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppColors.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                SelectionBackColor = AppColors.Primary,
                SelectionForeColor = Color.White,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0)
            };

            // Enable double buffering to prevent flickering
            typeof(DataGridView)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(this, true, null);
        }

        protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
        {
            base.OnCellFormatting(e);
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = Columns[e.ColumnIndex].Name.ToLower();
            if (colName.Contains("sửa") || colName.Contains("edit"))
            {
                e.CellStyle.ForeColor = AppColors.Primary;
                e.CellStyle.SelectionForeColor = AppColors.PrimaryDark;
                e.CellStyle.Font = _boldFont;
            }
            else if (colName.Contains("xóa") || colName.Contains("delete") || colName.Contains("remove"))
            {
                e.CellStyle.ForeColor = AppColors.Danger;
                e.CellStyle.SelectionForeColor = Color.FromArgb(185, 28, 28); // red-700
                e.CellStyle.Font = _boldFont;
            }
        }
    }
}
