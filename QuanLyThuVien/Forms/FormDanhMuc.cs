using QuanLyThuVien.Controls;
using QuanLyThuVien.Helpers;

namespace QuanLyThuVien.Forms
{
    public class FormDanhMuc : UserControl
    {
        public FormDanhMuc()
        {
            BackColor = AppColors.ContentBg;
            Padding = new Padding(25, 20, 25, 20);
            AutoScroll = true;
            Load += (s, e) => BuildUI();
        }

        private void BuildUI()
        {
            Controls.Clear();

            Controls.Add(new Label
            {
                Text = "Danh mục",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(5, 15)
            });

            var cards = new[]
            {
                ("Thể loại", "Quản lý thể loại sách", AppColors.CardBlue, (Action)(() => LoadSub(new FormTheLoai()))),
                ("Tác giả", "Quản lý thông tin tác giả", AppColors.CardGreen, () => LoadSub(new FormTacGia())),
                ("Nhà xuất bản", "Quản lý nhà xuất bản", AppColors.CardOrange, () => LoadSub(new FormNhaXuatBan())),
            };

            int x = 0;
            int cardW = 220;
            int cardH = 140;
            int spacing = 20;

            foreach (var (title, desc, color, action) in cards)
            {
                var card = new Panel
                {
                    Size = new Size(cardW, cardH),
                    Location = new Point(Padding.Left + x, 70),
                    BackColor = Color.White,
                    Cursor = Cursors.Hand
                };

                var accent = new Panel
                {
                    Size = new Size(4, cardH),
                    Location = new Point(0, 0),
                    BackColor = color,
                    Dock = DockStyle.Left
                };

                var lblTitle = new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                    ForeColor = AppColors.TextPrimary,
                    AutoSize = true,
                    Location = new Point(20, 25)
                };

                var lblDesc = new Label
                {
                    Text = desc,
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = AppColors.TextSecondary,
                    AutoSize = true,
                    Location = new Point(20, 55)
                };

                var lblArrow = new Label
                {
                    Text = "\u2192",
                    Font = new Font("Segoe UI", 16F),
                    ForeColor = AppColors.TextSecondary,
                    AutoSize = true,
                    Location = new Point(cardW - 40, cardH / 2 - 15)
                };

                card.Controls.AddRange(new Control[] { accent, lblTitle, lblDesc, lblArrow });

                card.Click += (s, e) => action();
                foreach (Control c in card.Controls)
                    c.Click += (s, e) => action();

                Controls.Add(card);
                x += cardW + spacing;
            }
        }

        private void LoadSub(UserControl uc)
        {
            uc.Dock = DockStyle.Fill;
            Parent?.Controls.Add(uc);
            uc.BringToFront();
            Visible = false;
            uc.Tag = this;
        }
    }
}
