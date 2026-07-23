using QuanLyThuVien.Controls;

namespace QuanLyThuVien.Helpers
{
    /// <summary>
    /// Shared layout helpers for screens that must remain usable when the main window is resized.
    /// </summary>
    public static class ResponsiveUi
    {
        public static void DisposeChildren(Control parent)
        {
            var children = parent.Controls.Cast<Control>().ToArray();
            parent.Controls.Clear();
            foreach (var child in children) child.Dispose();
        }

        public static Panel AddListGridHost(Control owner, DataGridView grid, int top = 105)
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, top, 10, 10),
                BackColor = Color.Transparent
            };
            grid.Dock = DockStyle.Fill;
            host.Controls.Add(grid);
            owner.Controls.Add(host);
            host.SendToBack();
            return host;
        }

        public static PageHeader AddListPage(
            Control owner,
            DataGridView grid,
            string title,
            params Control[] actions)
        {
            var page = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = Padding.Empty
            };
            var header = new PageHeader(title, actions);
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = Padding.Empty
            };

            grid.Dock = DockStyle.Fill;
            host.Controls.Add(grid);
            page.Controls.Add(host);
            page.Controls.Add(header);
            owner.Controls.Add(page);
            return header;
        }

        public static void AddFilterBar(PageHeader header, FilterBar filterBar)
        {
            header.SetFilterBar(filterBar);
        }

        public static Form CreateDialog(string title, Size clientSize, Size minimumSize)
        {
            return new Form
            {
                Text = title,
                ClientSize = clientSize,
                MinimumSize = minimumSize,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                MinimizeBox = false,
                AutoScaleMode = AutoScaleMode.Font,
                BackColor = AppColors.ContentBg
            };
        }

        public static FlowLayoutPanel AddDialogFooter(Form form, int height = 64)
        {
            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = height,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = AppColors.HeaderBg
            };
            form.Controls.Add(footer);
            return footer;
        }
    }
}
