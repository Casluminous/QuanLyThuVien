# Responsive Book Catalog Grid Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Kho sach catalog add or remove fixed-size book-card columns as the available window width changes.

**Architecture:** Keep layout ownership in `FormSach.LayoutCatalog`. The method derives a safe column count from the catalog width, reserves the vertical-scrollbar width only when the calculated content needs it, assigns every existing `BookCardControl` its fixed visual size, and positions cards in row-major order. `AutoScrollMinSize` carries the calculated content height so overflow remains vertical only.

**Tech Stack:** C# 10, .NET 10 WinForms, existing `Panel` and `BookCardControl` controls.

---

## File structure

- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormSach.cs` — calculate the responsive column count and catalog content height.
- Verify: `D:\QuanLyThuVien\QuanLyThuVien.slnx` — build the existing WinForms project. There is no checked-in unit-test project, so the UI behavior is verified in the running application.

### Task 1: Make the catalog grid responsive

**Files:**
- Modify: `D:\QuanLyThuVien\QuanLyThuVien\Forms\FormSach.cs:221-239`
- Test: Manual resize check in the running `QuanLyThuVien.exe`

- [ ] **Step 1: Replace the fixed three-column calculation with a width-based calculation**

Replace `LayoutCatalog` with the following implementation. It preserves the current 240 x 340 card design, guarantees at least one column for a narrow or not-yet-measured panel, and never requests a horizontal content width.

```csharp
private void LayoutCatalog()
{
    if (pnlCatalog == null)
        return;

    const int cardWidth = 240;
    const int cardHeight = 340;
    const int spacingX = 20;
    const int spacingY = 20;
    const int startX = 10;
    const int startY = 10;

    if (pnlCatalog.Controls.Count == 0)
    {
        pnlCatalog.AutoScrollMinSize = Size.Empty;
        return;
    }

    int GetColumnCount(int panelWidth)
    {
        int availableWidth = Math.Max(0, panelWidth - startX * 2);
        return Math.Max(1, (availableWidth + spacingX) / (cardWidth + spacingX));
    }

    int GetContentHeight(int columns)
    {
        int rowCount = (pnlCatalog.Controls.Count + columns - 1) / columns;
        return startY * 2 + rowCount * cardHeight + (rowCount - 1) * spacingY;
    }

    int cols = GetColumnCount(pnlCatalog.Width);
    int contentHeight = GetContentHeight(cols);

    if (contentHeight > pnlCatalog.Height)
    {
        cols = GetColumnCount(pnlCatalog.Width - SystemInformation.VerticalScrollBarWidth);
        contentHeight = GetContentHeight(cols);
    }

    for (int i = 0; i < pnlCatalog.Controls.Count; i++)
    {
        int row = i / cols;
        int col = i % cols;
        Control card = pnlCatalog.Controls[i];
        card.Size = new Size(cardWidth, cardHeight);
        card.Location = new Point(
            startX + col * (cardWidth + spacingX),
            startY + row * (cardHeight + spacingY));
    }

    pnlCatalog.AutoScrollMinSize = new Size(0, contentHeight);
}
```

- [ ] **Step 2: Build the solution**

Run:

```powershell
dotnet build D:\QuanLyThuVien\QuanLyThuVien.slnx
```

Expected: build succeeds with zero errors.

- [ ] **Step 3: Verify the resize behavior in the actual application**

Launch `D:\QuanLyThuVien\QuanLyThuVien\bin\Debug\net10.0-windows\QuanLyThuVien.exe`, sign in, and open **Kho sach**. Confirm each condition:

1. At ordinary width, the catalog keeps 240 x 340 cards with even gaps.
2. Maximize the application: more cards appear in each row than at ordinary width.
3. Narrow the application until only one card fits: cards remain entirely visible, align to the left padding, and no horizontal scrollbar appears.
4. With enough books to exceed the panel height, the vertical scrollbar reaches all later rows.

- [ ] **Step 4: Commit the verified implementation**

```powershell
git -C D:\QuanLyThuVien add -- QuanLyThuVien/Forms/FormSach.cs
git -C D:\QuanLyThuVien commit -m "feat: make book catalog grid responsive"
```

## Self-review

- Spec coverage: Task 1 retains fixed card sizing, derives columns from the visible width, recalculates through the existing `Resize` event, preserves vertical scrolling, and explicitly tests narrow and maximized states.
- Placeholder scan: no placeholders or deferred implementation steps remain.
- Type consistency: `pnlCatalog` is the existing `Panel`; `Size`, `Point`, and `Control` are already available through the project's WinForms implicit usings.
