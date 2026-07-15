using QuanLyThuVien.Data;
using QuanLyThuVien.Forms;

namespace QuanLyThuVien
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            try
            {
                if (!DataAccess.HasAnyNhanVien())
                {
                    var firstRun = new FormFirstRun();
                    if (firstRun.ShowDialog() != DialogResult.OK)
                        return;
                }
            }
            catch
            {
                MessageBox.Show("Không thể kết nối cơ sở dữ liệu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new FormDangNhap());
        }
    }
}
