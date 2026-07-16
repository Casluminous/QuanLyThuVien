# Date Validation and Stock Clarity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent invalid lending and reader-card dates while making the existing available-stock meaning explicit.

**Architecture:** The loan form constrains input for immediate feedback, while `DataAccess.InsertPhieuMuonFull` validates dates before it opens a transaction. The reader form keeps its two related date inputs consistent and validates on save. The book-edit label clarifies the existing storage model without a migration.

**Tech Stack:** C# 14, .NET 10 WinForms, SQL Server via Microsoft.Data.SqlClient.

---

### Task 1: Guard loan dates

**Files:**
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormPhieuMuon.cs:178-247`
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Data\DataAccess.cs:305-313`

- [x] **Step 1: Constrain the dialog date and validate before submission**

```csharp
var today = DateTime.Today;
var dtpMuon = new DateTimePicker
{
    Location = new Point(140, 62),
    Size = new Size(200, 30),
    Format = DateTimePickerFormat.Short,
    MaxDate = today,
    Value = today
};
if (dtpMuon.Value.Date > today)
{
    MessageBox.Show("Ngày mượn không được ở tương lai!");
    return;
}
```

- [x] **Step 2: Validate at the data-access boundary before opening the connection**

```csharp
if (pm.NgayMuon.Date > DateTime.Today)
{
    failureReason = "Ngày mượn không được ở tương lai.";
    return false;
}
if (pm.HanTra.Date < pm.NgayMuon.Date)
{
    failureReason = "Hạn trả không được trước ngày mượn.";
    return false;
}
```

- [x] **Step 3: Build the solution**

Run: `dotnet build D:\QuanLyThuVien\QuanLyThuVien.slnx --no-restore`

Expected: build succeeds with no errors.

### Task 2: Guard reader-card dates and clarify stock input

**Files:**
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormDocGia.cs:168-190`
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormSach.cs:610-611`

- [x] **Step 1: Synchronize issue and expiry date controls**

```csharp
void EnsureExpiryIsValid()
{
    if (dtpHSD.Value.Date < dtpLT.Value.Date)
        dtpHSD.Value = dtpLT.Value.Date;
    dtpHSD.MinDate = dtpLT.Value.Date;
}
dtpLT.ValueChanged += (s, e) => EnsureExpiryIsValid();
EnsureExpiryIsValid();
```

- [x] **Step 2: Reject an invalid relationship at save time**

```csharp
if (dtpHSD.Value.Date < dtpLT.Value.Date)
{
    MessageBox.Show("Hạn sử dụng không được trước ngày lập thẻ!");
    return;
}
```

- [x] **Step 3: Rename the book edit label**

```csharp
Text = "Số lượng còn sẵn:"
```

- [x] **Step 4: Build and inspect the focused diff**

Run: `dotnet build D:\QuanLyThuVien\QuanLyThuVien.slnx --no-restore; git -C D:\QuanLyThuVien diff --check`

Expected: build succeeds with no errors and no whitespace errors.

### Task 3: Commit verified code

**Files:**
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Data\DataAccess.cs`
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormPhieuMuon.cs`
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormDocGia.cs`
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormSach.cs`

- [x] **Step 1: Commit the completed change**

```powershell
git -C D:\QuanLyThuVien add QuanLyThuVien\Data\DataAccess.cs QuanLyThuVien\Forms\FormPhieuMuon.cs QuanLyThuVien\Forms\FormDocGia.cs QuanLyThuVien\Forms\FormSach.cs docs\superpowers\plans\2026-07-16-date-validation-and-stock-clarity.md
git -C D:\QuanLyThuVien commit -m "fix: validate lending and card dates"
```
