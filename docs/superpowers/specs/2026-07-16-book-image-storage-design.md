# Book Image Asset Storage Design

## Goal

When a user selects or crops a book cover, the application must create and use its own local asset copy. Deleting or moving the originally selected file must not make the cover unavailable.

## Scope

This design changes every book-cover write path in `FormSach`:

- add and edit book dialogs;
- the cover-detail dialog's crop-and-save flow; and
- all book-cover display paths.

It does not migrate every existing database row immediately and it does not delete old image assets automatically.

## Current State

`FormSach` currently uses `%AppData%\\QuanLyThuVien\\Images\\Sach` as a copy target, but the `Sach.HinhAnh` column is treated as an absolute path. The add/edit flow also suppresses copy failures and can then persist the original path. The result is not a reliable, portable asset reference.

## Chosen Architecture

### Asset root and database format

New book-cover files will be stored below the application's runtime directory:

```text
<application directory>\\Images\\Sach\\<guid>.<extension>
```

For example, a Debug build stores files in:

```text
D:\\QuanLyThuVien\\QuanLyThuVien\\bin\\Debug\\net10.0-windows\\Images\\Sach
```

The database will store only a relative asset key such as:

```text
Sach/3f3a63e709af4e6db0f16a07813c0d4e.png
```

The runtime directory is resolved with `AppContext.BaseDirectory`; display code must never reconstruct paths from an originally selected source file.

### Storage helper

Introduce one focused helper responsible for book-cover persistence and resolution. It will:

1. Create `Images\\Sach` when needed.
2. Validate that a chosen file is a readable image before copying it.
3. Copy a selected image with a GUID filename while preserving its supported extension.
4. Save cropped output as a GUID-named JPEG.
5. Return the relative asset key for database persistence.
6. Resolve a relative asset key to a full path for rendering.
7. Treat an existing absolute path as a read-only legacy fallback.

The helper never writes the original selected path to the database.

### Form behavior

The add/edit dialog keeps the selected source path only while the dialog is open. On **Lưu**, it copies the image, receives a relative asset key, then saves that key through `DataAccess.InsertSach` or `DataAccess.UpdateSach`.

The crop flow follows the same storage helper rather than assembling its own absolute path. When no new image is selected during an edit, the existing key remains unchanged.

All cover preview, catalog-card, and detail-view reads use the resolver. New relative keys therefore load from the local asset folder, while existing valid absolute paths remain visible until a later update replaces them.

## Failure Handling

- A failed image read, validation, directory creation, or copy produces a clear user-facing error and stops the save; the original path is not persisted.
- If the database write fails after creating a new asset, the application makes a best-effort deletion of that newly created asset and reports the database error.
- Invalid or unsupported files are rejected before any database write.
- Image streams are cloned or disposed correctly so the source file is not locked.

## Data Lifecycle

- Replacing a cover creates a new asset and updates the book's key.
- The previous asset is retained; automatic deletion is deliberately out of scope to avoid removing a file that may still be referenced by legacy or manually edited data.
- Existing absolute-path values are not rewritten in bulk. Selecting a replacement cover transitions that book to the new relative-key format.

## Verification

The implementation must prove the following:

1. Adding a book with a selected image creates exactly one GUID-named file under the runtime `Images\\Sach` folder and stores a relative key in `Sach.HinhAnh`.
2. After deleting the originally selected source image, the new book cover remains visible in the catalog and detail dialog.
3. Editing a book without changing its cover preserves its existing image key.
4. Updating or cropping a cover stores and displays the local asset copy.
5. A failed copy does not write the original source path to the database.
6. Existing valid absolute image paths continue to display as a compatibility fallback.
