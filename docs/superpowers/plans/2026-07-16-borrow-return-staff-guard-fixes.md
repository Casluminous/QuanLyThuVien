# Borrow, Return, and Staff Guard Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Block loans for expired or inactive readers, correct the last-admin deletion guard, and preserve the return dialog when its transaction cannot complete.

**Architecture:** `DataAccess` supplies an eligible-reader query and repeats eligibility validation inside the existing loan transaction, returning a specific failure message to the form. The three WinForms forms make narrowly scoped UI decisions from that result; no schema, infrastructure, or new test project is added.

**Tech Stack:** C# 10, .NET 10 WinForms, System.Data.SqlClient, SQL Server Express.

---

## File structure

- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Data\DataAccess.cs` — eligible-reader query and transaction-level validation.
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormPhieuMuon.cs` — show only eligible readers and display the returned validation message.
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormPhieuTra.cs` — retain the dialog when the return transaction fails.
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormNhanVien.cs` — apply the last-admin guard only to the selected active administrator.

### Task 1: Enforce reader eligibility for new loans

**Files:**
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Data\DataAccess.cs:255-357`
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormPhieuMuon.cs:168-260`
- Test: Manual loan-dialog verification

- [ ] **Step 1: Add a query for the reader selector**

Add this method before the `PhieuMuon` section in `DataAccess.cs`:

```csharp
public static DataTable GetBorrowEligibleReaders() =>
    ExecuteQuery("SELECT * FROM DocGia WHERE TrangThai=1 AND HanSuDung>=CAST(GETDATE() AS DATE)");
```

Replace `DataAccess.GetAllDocGia()` in `FormPhieuMuon.ShowInputDialog` with `DataAccess.GetBorrowEligibleReaders()` and add every returned reader to `cboDG` without a second `TrangThai` condition.

- [ ] **Step 2: Return a reason when a loan transaction cannot start**

Replace `InsertPhieuMuonFull` with this complete implementation:

```csharp
public static bool InsertPhieuMuonFull(
    PhieuMuon pm,
    List<(int maSach, int soLuong)> chiTiet,
    out string? failureReason)
{
    failureReason = null;
    using (var conn = GetConnection())
    {
        conn.Open();
        using (var tran = conn.BeginTransaction())
        {
            try
            {
                using (var cmdReader = new SqlCommand(@"SELECT 1
                    FROM DocGia WITH (UPDLOCK, HOLDLOCK)
                    WHERE MaDG=@ma AND TrangThai=1 AND HanSuDung>=CAST(GETDATE() AS DATE)", conn, tran))
                {
                    cmdReader.Parameters.AddWithValue("@ma", pm.MaDG);
                    if (cmdReader.ExecuteScalar() == null)
                    {
                        failureReason = "Thẻ độc giả đã hết hạn hoặc không còn hoạt động.";
                        tran.Rollback();
                        return false;
                    }
                }

                int maPM;
                using (var cmd = new SqlCommand(@"INSERT INTO PhieuMuon(MaDG,MaNV,NgayMuon,HanTra,TrangThai)
                    VALUES(@madg,@manv,@nm,@ht,@tt); SELECT SCOPE_IDENTITY();", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@madg", pm.MaDG);
                    cmd.Parameters.AddWithValue("@manv", pm.MaNV);
                    cmd.Parameters.AddWithValue("@nm", pm.NgayMuon);
                    cmd.Parameters.AddWithValue("@ht", pm.HanTra);
                    cmd.Parameters.AddWithValue("@tt", pm.TrangThai);
                    maPM = Convert.ToInt32(cmd.ExecuteScalar());
                }

                foreach (var (maSach, soLuong) in chiTiet)
                {
                    using (var cmdStock = new SqlCommand("UPDATE Sach SET SoLuong=SoLuong-@sl WHERE MaSach=@ma AND SoLuong>=@sl", conn, tran))
                    {
                        cmdStock.Parameters.AddWithValue("@sl", soLuong);
                        cmdStock.Parameters.AddWithValue("@ma", maSach);
                        if (cmdStock.ExecuteNonQuery() == 0)
                        {
                            failureReason = "Không đủ tồn kho cho một hoặc nhiều sách.";
                            tran.Rollback();
                            return false;
                        }
                    }

                    using (var cmdDetail = new SqlCommand(@"INSERT INTO ChiTietPhieuMuon(MaPhieuMuon,MaSach,SoLuong)
                        VALUES(@mam,@mas,@sl)", conn, tran))
                    {
                        cmdDetail.Parameters.AddWithValue("@mam", maPM);
                        cmdDetail.Parameters.AddWithValue("@mas", maSach);
                        cmdDetail.Parameters.AddWithValue("@sl", soLuong);
                        cmdDetail.ExecuteNonQuery();
                    }
                }

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
    }
}
```

- [ ] **Step 3: Show the specific reason in the loan dialog**

Replace the current Boolean call with:

```csharp
bool ok = DataAccess.InsertPhieuMuonFull(pm, sachMuon, out string? failureReason);
if (!ok)
{
    MessageBox.Show(failureReason ?? "Không thể tạo phiếu mượn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
}
```

- [ ] **Step 4: Verify the loan behavior manually**

Open **Phiếu mượn**. Confirm a reader whose `HanSuDung` is before today is absent from the selector; an active, unexpired reader remains selectable; and a normal in-stock loan succeeds.

### Task 2: Correct staff deletion and return-failure UI behavior

**Files:**
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormNhanVien.cs:119-137`
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormPhieuTra.cs:204-208`
- Test: Manual management and return-dialog verification

- [ ] **Step 1: Restrict the last-admin guard to the selected active admin**

Inside the delete branch of `FormNhanVien.Dgv_CellClick`, replace the unconditional count check with:

```csharp
var selectedRow = dgv.Rows[e.RowIndex];
bool deletingActiveAdmin =
    selectedRow.Cells["VaiTro"].Value?.ToString() == "Admin" &&
    selectedRow.Cells["TrangThai"].Value?.ToString() == "Hoạt động";

if (deletingActiveAdmin && DataAccess.CountActiveAdmins() <= 1)
{
    MessageBox.Show("Không thể xóa admin cuối cùng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    return;
}
```

Keep the existing self-delete guard unchanged.

- [ ] **Step 2: Keep the return dialog open on transaction failure**

Replace the unconditional close/reload with:

```csharp
try
{
    if (!DataAccess.TraNhieuSach(maPM, items))
    {
        MessageBox.Show("Không thể xác nhận trả sách vì dữ liệu đã thay đổi. Vui lòng tải lại danh sách.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }
}
catch (Exception ex)
{
    MessageBox.Show("Không thể xác nhận trả sách: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return;
}

frm.Close();
LoadData();
```

- [ ] **Step 3: Build and manually verify both changes**

Run:

```powershell
dotnet build D:\QuanLyThuVien\QuanLyThuVien.slnx
```

Expected: build succeeds with zero errors. In the application, verify an ordinary staff member can be deleted when one active administrator remains. For the return dialog, keep it open when `TraNhieuSach` returns `false` and close it only after success.

- [ ] **Step 4: Commit the verified fix**

```powershell
git -C D:\QuanLyThuVien add -- QuanLyThuVien/Data/DataAccess.cs QuanLyThuVien/Forms/FormPhieuMuon.cs QuanLyThuVien/Forms/FormPhieuTra.cs QuanLyThuVien/Forms/FormNhanVien.cs docs/superpowers/plans/2026-07-16-borrow-return-staff-guard-fixes.md
git -C D:\QuanLyThuVien commit -m "fix: enforce loan and staff safeguards"
```

## Self-review

- Spec coverage: Task 1 filters the reader selector and revalidates eligibility before stock changes; Task 2 scopes the administrator guard correctly and respects return transaction failure.
- Placeholder scan: all code, messages, paths, commands, and manual checks are explicit.
- Type consistency: `failureReason` is a nullable `string` passed as an `out` parameter; all callers use the same `InsertPhieuMuonFull` signature.
