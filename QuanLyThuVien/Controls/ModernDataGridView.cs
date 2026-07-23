using System.Reflection;
using QuanLyThuVien.Helpers;

using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace QuanLyThuVien.Controls
{
    public class ModernDataGridView : DataGridView
    {
        private readonly Font _boldFont;
        private int _hoveredRow = -1;
        private int _sortColumnIndex = -1;
        private SortOrder _sortOrder = SortOrder.None;

        public ModernDataGridView()
        {
            _boldFont = new Font(DefaultCellStyle.Font ?? SystemFonts.DefaultFont, FontStyle.Bold);
            // Set styles for flat modern look
            BackgroundColor = AppColors.CardBg;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            GridColor = AppColors.Border;

            // Configuration
            RowHeadersVisible = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            AllowUserToResizeColumns = true;
            ReadOnly = true;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            ScrollBars = ScrollBars.Both;
            EnableHeadersVisualStyles = false;

            // Row heights
            RowTemplate.Height = 40;
            ColumnHeadersHeight = 44;

            // Default Cell Style (slate-700 text)
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = AppColors.TextPrimary,
                BackColor = AppColors.CardBg,
                SelectionBackColor = AppColors.SelectedSurface,
                SelectionForeColor = AppColors.TextPrimary,
                Padding = new Padding(8, 0, 8, 0)
            };

            // Alternating Row Style (slate-50 background)
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = AppColors.TextPrimary,
                BackColor = AppColors.AlternateSurface,
                SelectionBackColor = AppColors.SelectedSurface,
                SelectionForeColor = AppColors.TextPrimary,
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

            ColumnAdded += ModernDataGridView_ColumnAdded;
            ColumnHeaderMouseClick += ModernDataGridView_ColumnHeaderMouseClick;
            CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && _hoveredRow != e.RowIndex)
                {
                    _hoveredRow = e.RowIndex;
                    InvalidateRow(e.RowIndex);
                }
            };
            CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0 && _hoveredRow == e.RowIndex)
                {
                    _hoveredRow = -1;
                    InvalidateRow(e.RowIndex);
                }
            };
        }

        private void ModernDataGridView_ColumnAdded(object? sender, DataGridViewColumnEventArgs e)
        {
            string name = e.Column.Name.ToLowerInvariant();
            bool action = name.StartsWith("btn") || name is "sua" or "sửa" or "xoa" or "xóa" or
                          "delete" or "remove" or "tra" or "chitiet" || name.Contains("chitiet") || name.Contains("lichsu");
            if (action)
            {
                e.Column.SortMode = DataGridViewColumnSortMode.NotSortable;
                e.Column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                e.Column.Width = 92;
                e.Column.MinimumWidth = 84;
                e.Column.FillWeight = 1;
            }
            else if (name.Contains("ma") || name.Contains("id"))
            {
                e.Column.SortMode = DataGridViewColumnSortMode.Programmatic;
                e.Column.MinimumWidth = 70;
                e.Column.FillWeight = 12;
            }
            else
            {
                e.Column.SortMode = DataGridViewColumnSortMode.Programmatic;
                e.Column.MinimumWidth = 110;
                e.Column.FillWeight = 20;
            }
        }

        private void ModernDataGridView_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.ColumnIndex >= Columns.Count) return;
            var column = Columns[e.ColumnIndex];
            if (column.SortMode == DataGridViewColumnSortMode.NotSortable) return;

            _sortOrder = _sortColumnIndex == e.ColumnIndex && _sortOrder == SortOrder.Ascending
                ? SortOrder.Descending
                : SortOrder.Ascending;
            _sortColumnIndex = e.ColumnIndex;

            foreach (DataGridViewColumn item in Columns)
                item.HeaderCell.SortGlyphDirection = SortOrder.None;
            column.HeaderCell.SortGlyphDirection = _sortOrder;

            try
            {
                Sort(new RowComparer(e.ColumnIndex, _sortOrder == SortOrder.Ascending));
            }
            catch (InvalidOperationException)
            {
                // Bound grids manage sorting through their data source.
            }
        }

        protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
        {
            base.OnCellFormatting(e);
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = Columns[e.ColumnIndex].Name.ToLowerInvariant();
            if (e.RowIndex == _hoveredRow && !Rows[e.RowIndex].Selected)
                e.CellStyle.BackColor = AppColors.HoverSurface;

            // Forms may replace DefaultCellStyle after this control is created.
            // Apply the effective body style here so alternating rows never shift
            // horizontally because of a different padding value.
            e.CellStyle.Padding = new Padding(10, 0, 10, 0);
            e.CellStyle.WrapMode = DataGridViewTriState.False;

            string normalizedName = Columns[e.ColumnIndex].Name.ToLowerInvariant();
            if (normalizedName.StartsWith("btn") || normalizedName.Contains("sửa") || normalizedName.Contains("xóa") || normalizedName == "tra" || normalizedName.Contains("chitiet") || normalizedName.Contains("lichsu"))
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            else if (normalizedName.Contains("tien") || normalizedName.Contains("gia"))
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            else if (normalizedName.Contains("ma") || normalizedName.Contains("id") || normalizedName.StartsWith("so") || normalizedName.Contains("ngay") || normalizedName.Contains("han") || normalizedName.Contains("trangthai"))
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            else
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            if (colName.Contains("sửa") || colName.Contains("edit"))
            {
                e.CellStyle.ForeColor = AppColors.Primary;
                e.CellStyle.SelectionForeColor = AppColors.PrimaryDark;
                e.CellStyle.Font = _boldFont;
            }
            else if (colName.Contains("xóa") || colName.Contains("delete") || colName.Contains("remove"))
            {
                e.CellStyle.ForeColor = AppColors.Danger;
                e.CellStyle.SelectionForeColor = AppColors.Danger;
                e.CellStyle.Font = _boldFont;
            }
            else if (colName.StartsWith("btn") || colName is "tra" or "chitiet" || colName.Contains("lichsu"))
            {
                e.CellStyle.ForeColor = AppColors.Primary;
                e.CellStyle.SelectionForeColor = AppColors.PrimaryDark;
                e.CellStyle.Font = _boldFont;
            }
        }

        private sealed class RowComparer : IComparer
        {
            private readonly int _columnIndex;
            private readonly bool _ascending;

            public RowComparer(int columnIndex, bool ascending)
            {
                _columnIndex = columnIndex;
                _ascending = ascending;
            }

            public int Compare(object? x, object? y)
            {
                var left = x as DataGridViewRow;
                var right = y as DataGridViewRow;
                if (ReferenceEquals(left, right)) return 0;
                if (left == null) return _ascending ? -1 : 1;
                if (right == null) return _ascending ? 1 : -1;

                int result = CompareValues(left.Cells[_columnIndex].Value, right.Cells[_columnIndex].Value);
                return _ascending ? result : -result;
            }

            private static int CompareValues(object? left, object? right)
            {
                if (left == null || left == DBNull.Value) return right == null || right == DBNull.Value ? 0 : -1;
                if (right == null || right == DBNull.Value) return 1;

                if (left is IComparable comparable && left.GetType() == right.GetType())
                    return comparable.CompareTo(right);

                string leftText = Convert.ToString(left, CultureInfo.CurrentCulture) ?? string.Empty;
                string rightText = Convert.ToString(right, CultureInfo.CurrentCulture) ?? string.Empty;
                if (decimal.TryParse(leftText, NumberStyles.Any, CultureInfo.CurrentCulture, out var leftNumber) &&
                    decimal.TryParse(rightText, NumberStyles.Any, CultureInfo.CurrentCulture, out var rightNumber))
                    return leftNumber.CompareTo(rightNumber);
                return StringComparer.CurrentCultureIgnoreCase.Compare(leftText, rightText);
            }
        }
    }
}
