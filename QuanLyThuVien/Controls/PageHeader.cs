using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Controls
{
    /// <summary>
    /// Shared responsive page header with a title on the left and actions on the right.
    /// </summary>
    public sealed class PageHeader : UserControl
    {
        private const int SingleRowHeight = 56;
        private const int HorizontalInset = 8;
        private readonly Label _titleLabel;
        private readonly FlowLayoutPanel _actionsPanel;
        private FilterBar? _filterBar;
        private bool _updatingLayout;

        public PageHeader(string title, params Control[] actions)
        {
            BackColor = Color.Transparent;
            Dock = DockStyle.Top;
            Height = SingleRowHeight;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
            TabStop = false;
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = $"Thanh thao tác {title}";

            _titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                TabStop = false
            };

            _actionsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                TabStop = false
            };

            Controls.Add(_titleLabel);
            Controls.Add(_actionsPanel);
            foreach (Control action in actions) AddAction(action);

            Resize += (_, _) => ArrangeChildren();
            ArrangeChildren();
        }

        public void AddAction(Control action)
        {
            action.Margin = new Padding(0, 0, 8, 0);
            action.TabIndex = _actionsPanel.Controls.Count;
            _actionsPanel.Controls.Add(action);
            ArrangeChildren();
        }

        public void SetFilterBar(FilterBar filterBar)
        {
            if (ReferenceEquals(_filterBar, filterBar)) return;

            if (_filterBar != null)
                Controls.Remove(_filterBar);

            _filterBar = filterBar;
            _filterBar.UseToolbarLayout();
            _filterBar.TabIndex = 0;
            _actionsPanel.TabIndex = 1;
            Controls.Add(_filterBar);
            _filterBar.BringToFront();
            ArrangeChildren();
        }

        public static ModernButton CreatePrimaryAction(string text, EventHandler onClick, int width = 140)
        {
            var button = new ModernButton
            {
                Text = text,
                Size = new Size(width, 40),
                BaseColor = AppColors.Primary,
                HoverColor = AppColors.PrimaryDark,
                PressedColor = AppColors.SidebarBg,
                BorderRadius = 12,
                AccessibleName = text.TrimStart('+', ' ')
            };
            button.Click += onClick;
            return button;
        }

        private void ArrangeChildren()
        {
            if (_updatingLayout || IsDisposed) return;
            _updatingLayout = true;
            try
            {
                const int verticalInset = 8;
                int availableWidth = Math.Max(0, ClientSize.Width - HorizontalInset * 2);
                int actionsWidth = _actionsPanel.Controls.Cast<Control>()
                    .Sum(control => control.Width + control.Margin.Horizontal);
                bool hasActions = actionsWidth > 0;
                int preferredTitleWidth = Math.Min(_titleLabel.PreferredSize.Width + 8, Math.Max(0, availableWidth));
                int filterWidth = _filterBar?.ToolbarPreferredWidth ?? 0;
                bool hasFilter = filterWidth > 0;
                int singleRowWidth = preferredTitleWidth
                    + (hasFilter ? 8 + filterWidth : 0)
                    + (hasActions ? 8 + actionsWidth : 0);
                bool filterOnSecondRow = hasFilter && singleRowWidth > availableWidth;
                bool actionsBelowTitle = hasActions && preferredTitleWidth + actionsWidth + 24 > availableWidth;
                int actionRows = 1;
                if (actionsBelowTitle && actionsWidth > availableWidth && availableWidth > 0)
                {
                    int rowWidth = 0;
                    foreach (Control action in _actionsPanel.Controls)
                    {
                        int actionWidth = action.Width + action.Margin.Horizontal;
                        if (rowWidth > 0 && rowWidth + actionWidth > availableWidth)
                        {
                            actionRows++;
                            rowWidth = 0;
                        }
                        rowWidth += actionWidth;
                    }
                }
                int firstBlockHeight = actionsBelowTitle ? 60 + actionRows * 48 : SingleRowHeight;
                int targetHeight = firstBlockHeight + (filterOnSecondRow ? 52 : 0);
                if (Height != targetHeight) Height = targetHeight;

                _titleLabel.Location = new Point(HorizontalInset, verticalInset);
                _titleLabel.Size = new Size(
                    Math.Max(0, actionsBelowTitle || !hasActions
                        ? (hasFilter && !filterOnSecondRow ? preferredTitleWidth : availableWidth)
                        : (hasFilter && !filterOnSecondRow ? preferredTitleWidth : availableWidth - actionsWidth - 20)),
                    40);

                if (!hasActions)
                {
                    _actionsPanel.Visible = false;
                }
                else
                {
                    _actionsPanel.Visible = true;
                    _actionsPanel.WrapContents = actionsBelowTitle && actionRows > 1;
                    _actionsPanel.Size = new Size(
                        actionsBelowTitle ? Math.Min(actionsWidth, availableWidth) : actionsWidth,
                        actionsBelowTitle ? actionRows * 48 : 40);
                    _actionsPanel.Location = actionsBelowTitle
                        ? new Point(HorizontalInset, 60)
                        : new Point(HorizontalInset + Math.Max(0, availableWidth - actionsWidth), verticalInset);
                }

                if (_filterBar != null)
                {
                    _filterBar.Visible = true;
                    _filterBar.Size = new Size(Math.Min(filterWidth, availableWidth), 36);
                    _filterBar.Location = filterOnSecondRow
                        ? new Point(HorizontalInset, firstBlockHeight + 8)
                        : new Point(_titleLabel.Right + 8, 10);
                }
            }
            finally
            {
                _updatingLayout = false;
            }
        }
    }
}
