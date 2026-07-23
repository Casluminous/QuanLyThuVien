namespace QuanLyThuVien.Models;

public enum DemoLoanScenario
{
    DangMuon,
    QuaHan,
    DaTraMotPhan,
    DaTra,
    CoSachMat
}

public sealed record DemoLoanScenarioRequest(
    int MaDG,
    DateTime NgayMuon,
    DateTime HanTra,
    IReadOnlyList<(int MaSach, int SoLuong)> ChiTiet,
    DemoLoanScenario Scenario,
    int LostQuantity = 0);
