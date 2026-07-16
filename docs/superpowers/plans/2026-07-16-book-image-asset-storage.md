# Book Image Asset Storage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every new or cropped book-cover image independent from its original source file by storing a local copy in `Images\Sach` beside the running application and persisting a relative asset key in SQL Server.

**Architecture:** `BookImageStorage` will own asset paths, image validation, file copying, cropped-image output, legacy absolute-path fallback, and rollback of a just-created local asset. `FormSach` will use it for add, edit, crop, catalog, preview, and detail flows; `DataAccess` will expose an image-only update method instead of leaving SQL in the form.

**Tech Stack:** .NET 10, Windows Forms, System.Drawing, System.Data.SqlClient, SQL Server Express.

---

## File Structure

- Create: `QuanLyThuVien/Helpers/BookImageStorage.cs` — runtime-local cover storage.
- Modify: `QuanLyThuVien/Data/DataAccess.cs` — image-only `Sach` update API.
- Modify: `QuanLyThuVien/Forms/FormSach.cs` — all cover write and display paths.
- Create temporarily, then remove: `work/book-image-storage-probe/BookImageStorageProbe.csproj` and `Program.cs` — deterministic regression harness.

### Task 1: Create a deterministic storage regression probe

**Files:**

- Create: `work/book-image-storage-probe/BookImageStorageProbe.csproj`
- Create: `work/book-image-storage-probe/Program.cs`
- Create: `QuanLyThuVien/Helpers/BookImageStorage.cs`

- [ ] **Step 1: Write the failing probe before the helper exists**

Create the temporary project file:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0-windows</TargetFramework>
        <UseWindowsForms>true</UseWindowsForms>
      </PropertyGroup>
      <ItemGroup>
        <ProjectReference Include="..\..\QuanLyThuVien\QuanLyThuVien.csproj" />
      </ItemGroup>
    </Project>

Create `Program.cs`:

    using System.Drawing;
    using System.Drawing.Imaging;
    using QuanLyThuVien.Helpers;

    var root = Path.Combine(Path.GetTempPath(), "qltv-image-probe-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try
    {
        var source = Path.Combine(root, "source.png");
        using (var bitmap = new Bitmap(2, 2))
            bitmap.Save(source, ImageFormat.Png);

        var storage = new BookImageStorage(root);
        var key = storage.ImportFile(source);
        var copiedPath = storage.ResolvePath(key) ?? throw new Exception("Relative key did not resolve.");
        if (Path.IsPathRooted(key)) throw new Exception("Database key must be relative.");
        if (!File.Exists(copiedPath)) throw new Exception("Local copy was not created.");
        if (storage.ResolvePath(source) != source) throw new Exception("Legacy absolute-path fallback failed.");

        File.Delete(source);
        if (!File.Exists(copiedPath)) throw new Exception("Local copy depended on the deleted source.");
        Console.WriteLine("PASS");
    }
    finally
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

Run:

    dotnet run --project work/book-image-storage-probe/BookImageStorageProbe.csproj

Expected: compilation fails because `BookImageStorage` has not been created.

- [ ] **Step 2: Implement the isolated storage helper**

Create `BookImageStorage` with this API:

    public sealed class BookImageStorage
    {
        public BookImageStorage(string? applicationBaseDirectory = null);
        public string ImportFile(string sourcePath);
        public string SaveCroppedImage(Image image);
        public string? ResolvePath(string? storedValue);
        public Image? LoadImage(string? storedValue);
        public void DeleteLocalAsset(string? assetKey);
    }

Its implementation must follow these rules:

1. The base directory defaults to `AppContext.BaseDirectory`; the asset directory is `<base>\Images\Sach`.
2. `ImportFile` accepts only `.jpg`, `.jpeg`, `.png`, `.bmp`, and `.gif`, opens the source as an image before copying, creates a GUID filename, and returns a forward-slash key such as `Sach/3f3a63e709af4e6db0f16a07813c0d4e.png`.
3. `SaveCroppedImage` writes a GUID-named JPEG and returns `Sach/<guid>.jpg`.
4. `ResolvePath` returns an existing absolute path only as a legacy fallback. Relative values must start with `Sach/`, must contain only a filename after that prefix, and may resolve only inside the local asset directory.
5. `LoadImage` returns a cloned `Bitmap`, so streams and source files are not locked.
6. `DeleteLocalAsset` ignores absolute/legacy values and deletes only a resolved local asset.
7. Unreadable, nonexistent, unsupported, or uncopiable images throw a clear `InvalidOperationException`; no error is swallowed.

- [ ] **Step 3: Run the probe after the implementation**

Run:

    dotnet run --project work/book-image-storage-probe/BookImageStorageProbe.csproj

Expected: `PASS`. This proves a relative database key resolves to a local copy after the original selected image is deleted, while legacy absolute paths remain readable.

### Task 2: Add an image-only data-layer API

**Files:**

- Modify: `QuanLyThuVien/Data/DataAccess.cs`

- [ ] **Step 1: Add the focused update method**

Place this next to `UpdateSach`:

    public static int UpdateSachImage(int maSach, string hinhAnh) =>
        ExecuteNonQuery(
            "UPDATE Sach SET HinhAnh=@ha WHERE MaSach=@ma",
            new SqlParameter("@ha", hinhAnh),
            new SqlParameter("@ma", maSach));

- [ ] **Step 2: Compile the data-layer change**

Run:

    dotnet build QuanLyThuVien.slnx

Expected: build succeeds and the crop flow can stop creating a raw `SqlCommand` in the form.

### Task 3: Use the storage helper in every cover path

**Files:**

- Modify: `QuanLyThuVien/Forms/FormSach.cs`

- [ ] **Step 1: Replace the AppData-specific state**

Replace `imagesDir` with:

    private readonly BookImageStorage _bookImageStorage = new();

Remove `Directory.CreateDirectory(imagesDir)` from `LoadData`; the helper creates its own directory immediately before a write.

- [ ] **Step 2: Make catalog, detail, and edit preview reads resolve asset keys**

Replace direct `File.Exists(hinhAnh)` and `FileStream` reads with the helper:

    var image = _bookImageStorage.LoadImage(storedImageValue);
    if (image != null)
    {
        var oldImage = pictureBox.Image;
        pictureBox.Image = image;
        oldImage?.Dispose();
    }

For catalog cards, create a thumbnail from the cloned image and dispose the full-size clone after assigning the thumbnail. This keeps new relative keys and legacy absolute paths readable without locking either file.

- [ ] **Step 3: Persist only a local relative key during add/edit**

Keep a selected source path in a local `selectedImagePath` variable. The read-only text box displays the chosen filename only; it is not the persistence source. On save use this control flow:

    var imageKey = existing?.HinhAnh ?? string.Empty;
    string? createdImageKey = null;
    try
    {
        if (!string.IsNullOrWhiteSpace(selectedImagePath))
        {
            createdImageKey = _bookImageStorage.ImportFile(selectedImagePath);
            imageKey = createdImageKey;
        }

        var sach = new Sach
        {
            MaSach = existing?.MaSach ?? 0,
            TenSach = txt1.Text.Trim(),
            MaISBN = txt2.Text.Trim(),
            MaTL = tl.Value,
            MaTG = tg.Value,
            MaNXB = nxb.Value,
            NamXB = (int)nudNam.Value,
            SoLuong = (int)nudSL.Value,
            GiaTien = nudGia.Value,
            MoTa = txtMoTa.Text.Trim(),
            HinhAnh = imageKey
        };

        if (existing != null) DataAccess.UpdateSach(sach);
        else DataAccess.InsertSach(sach);
    }
    catch (Exception ex)
    {
        _bookImageStorage.DeleteLocalAsset(createdImageKey);
        MessageBox.Show("Không thể lưu ảnh bìa: " + ex.Message,
            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

The save operation must never fall back to persisting `ofd.FileName`. Editing without selecting a new file preserves the existing key.

- [ ] **Step 4: Use the same contract for cropped covers**

Replace the crop handler's `Path.Combine(imagesDir, ...)` and inline SQL with:

    string? createdImageKey = null;
    try
    {
        createdImageKey = _bookImageStorage.SaveCroppedImage(pendingCroppedImage);
        DataAccess.UpdateSachImage(maSach, createdImageKey);
        sach.HinhAnh = createdImageKey;
        // Refresh pbCover with _bookImageStorage.LoadImage(createdImageKey).
    }
    catch (Exception ex)
    {
        _bookImageStorage.DeleteLocalAsset(createdImageKey);
        MessageBox.Show("Lỗi khi lưu ảnh: " + ex.Message,
            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

Dispose a replaced `PictureBox.Image` and the completed `pendingCroppedImage` after successful save. Cancelling crop or choosing no replacement preserves the original key.

- [ ] **Step 5: Compile the complete integration**

Run:

    dotnet build QuanLyThuVien.slnx

Expected: build succeeds with no reference to `imagesDir` and no direct SQL update for book covers in `FormSach`.

### Task 4: Verify the database, filesystem, and visible app flow

**Files:**

- Runtime only: `<application directory>/Images/Sach/*`
- Verify: `QuanLyThuVien.Sach.HinhAnh`

- [ ] **Step 1: Re-run the deterministic helper probe**

Run:

    dotnet run --project work/book-image-storage-probe/BookImageStorageProbe.csproj --no-build

Expected: `PASS`.

- [ ] **Step 2: Check the stored value after adding or updating a cover**

Run:

    sqlcmd -S '.\SQLEXPRESS' -E -C -d QuanLyThuVien -W -s '|' -Q "SELECT MaSach, TenSach, HinhAnh FROM Sach WHERE HinhAnh LIKE 'Sach/%' ORDER BY MaSach DESC;"

Expected: the value starts with `Sach/`, contains no original drive path, and resolves to a file beneath `Images\Sach`.

- [ ] **Step 3: Run the visible end-to-end regression**

1. Launch `QuanLyThuVien.exe` and sign in with `admin` / `admin`.
2. Add a book using a disposable PNG or JPEG outside the application directory.
3. Confirm the cover appears in the catalog and detail dialog.
4. Delete the original selected image.
5. Reload the catalog and detail dialog; the cover must remain visible.
6. Replace the cover through the crop flow and confirm it remains visible after reopening the book detail.

- [ ] **Step 4: Remove the temporary probe**

Delete only `work/book-image-storage-probe` after it passes, including generated `bin` and `obj`. Verify the resolved deletion target is inside the workspace `work` directory before removing it.

### Task 5: Review and commit

**Files:**

- Review: `QuanLyThuVien/Helpers/BookImageStorage.cs`
- Review: `QuanLyThuVien/Data/DataAccess.cs`
- Review: `QuanLyThuVien/Forms/FormSach.cs`

- [ ] **Step 1: Inspect the focused diff and prohibit source-path persistence**

Run:

    git diff --check
    git diff -- QuanLyThuVien/Helpers/BookImageStorage.cs QuanLyThuVien/Data/DataAccess.cs QuanLyThuVien/Forms/FormSach.cs
    rg -n "File.Copy\(|HinhAnh = ofd.FileName|imagesDir|UPDATE Sach SET HinhAnh" QuanLyThuVien/Forms/FormSach.cs

Expected: all upload writes route through `BookImageStorage` or `DataAccess.UpdateSachImage`; no upload copy exception is silently ignored.

- [ ] **Step 2: Commit the verified implementation**

Run:

    git add QuanLyThuVien/Helpers/BookImageStorage.cs QuanLyThuVien/Data/DataAccess.cs QuanLyThuVien/Forms/FormSach.cs docs/superpowers/plans/2026-07-16-book-image-asset-storage.md
    git commit -m "feat: persist book cover assets locally"

Expected: the verified feature and implementation plan are isolated from unrelated changes.
