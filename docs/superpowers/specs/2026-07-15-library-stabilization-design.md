# Stabilize QuanLyThuVien

**Date:** 2026-07-15  
**Status:** Design approved; implementation requires review of this spec.  
**Scope:** Every reproducible current defect identified by code inspection and the supplied review. Findings whose code has already changed are verified, not reimplemented.

## Goals

1. Preserve and protect the live `QuanLyThuVien` database while making schema upgrades repeatable.
2. Ensure authentication, employee management, borrowing, and returning books preserve data invariants.
3. Restore safe, role-appropriate navigation and correct UI behavior.
4. Make image storage portable, reporting correct, and control/image lifetimes bounded.
5. Add automated coverage for pure logic and repeatable database verification.

## Current-state reconciliation

The supplied review predates several source changes. The current source already uses versioned PBKDF2 with legacy SHA-256 verification, creates all navigation items, wires loan-detail clicks, disposes controls when changing views, and loads book foreign keys before editing. The implementation will retain and test those fixes.

Defects still present include the `TacGia.QuocTich`/`QuocTia` schema mismatch, a 64-character password column in the bootstrap script despite 83-character PBKDF2 records, hard-coded connection settings, unguarded/non-repeatable database seeding, unsafe legacy loan APIs, default selection of every available book, incomplete authorization, image paths under build output, image stream lifetime issues, reporting inaccuracies, and recurring event handlers.

## Database safety and migration

Before changing live data, the migration runner will:

1. Connect using configured settings and perform read-only preflight checks for database name, schema version, backup path writeability, and invalid legacy data.
2. Create and verify a checksum-protected `COPY_ONLY` database backup. A failed backup or preflight stops all changes.
3. Apply one ordered migration in an explicit transaction where SQL Server permits transactional DDL. The migration records a version and checksum in `SchemaMigrations` only after success.
4. Run post-migration invariant queries and report row counts and repaired values. It never drops the database, tables, or user data.

The migration will reconcile `TacGia.QuocTich` without losing existing values; widen `NhanVien.MatKhau` to at least `VARCHAR(256)`; repair the `PhieuMuon.TrangThai` default and known mojibake value; add check constraints for nonnegative stock, price, penalty, and positive loan detail quantity; and make required indexes/defaults explicit. It will stop rather than silently coerce a row that violates a new invariant.

`database.sql` becomes a repeatable new-install schema script. It will create schema objects only when absent, creates no predictable administrator credential, and keeps optional demo data in a separate idempotent seed script. A first-run setup form creates the first administrator only when no employee exists.

## Application architecture

### Configuration and authentication

`DataAccess` will obtain its connection string from a configuration file with an environment-variable override, rather than literals duplicated across forms. The UI shows a safe actionable database error while diagnostic detail is written to a local application log.

PBKDF2 remains the only format written. A successful login using the supported legacy SHA-256 verifier immediately rehashes that password with PBKDF2. Parsing malformed stored hashes returns a failed login rather than crashing. The new-install flow never displays or embeds a default password.

### Authorization

Employee administration is visible only to admins and every employee-management command performs an admin check before reaching SQL. The application prevents deleting the current account or removing its final active administrator. Other navigation remains available according to its documented library-staff capability.

### Borrowing and returning

The only public loan path creates the loan header, validates a nonempty unique set of positive book quantities, decrements each book only when sufficient stock exists, inserts every detail, and commits once. Any failure rolls back the entire loan. Legacy APIs that could commit a header or detail independently will be removed or made private.

The loan UI uses an explicit unchecked selection column and a default quantity of zero, so creating a loan cannot accidentally borrow every listed book.

Returning selected loan details is a single transaction. Each unreturned detail is marked returned exactly once and stock is incremented only for successfully updated details. The loan status changes to `Đã trả` only if no unreturned details remain; otherwise it remains open. The UI reports a conflict rather than claiming success when a concurrent return has already modified a detail.

### Catalog, images, reports, and UI lifetime

Author CRUD consistently uses `QuocTich`. Book edit paths retain foreign-key IDs. New book images receive GUID names in a writable application-data image directory; the database stores a relative asset key, while old absolute paths are supported as a read-only fallback. Images loaded from a stream are cloned before that stream closes.

Dashboard/report queries use the selected/current year correctly and use `SUM(SoLuong)` for copy quantities. Placeholder labels are replaced. Forms rely on anchoring instead of re-attaching `Resize` handlers on every reload, dispose timers/images when disposed, and replace silent catches with logged, user-safe errors.

## Dependency and testing strategy

After core behavior passes, package references are audited against official package metadata and upgraded only to versions compatible with `net10.0-windows`; the goal is to remove the current NU1701 compatibility warnings without changing application behavior.

A test project will cover PBKDF2 and legacy migration, authorization guards, selected-book validation, image-path resolution, and report calculations. Database verification runs against a disposable test database and exercises rerunnable migration, insufficient stock, multi-book loan rollback, duplicate/concurrent return, and schema invariants. A manual smoke checklist covers login, role menus, catalog edits, borrow/return, reports, and image persistence.

## Acceptance criteria

- A preflighted backup exists and the live migration is recorded once; rerunning it makes no duplicate data or schema changes.
- Existing PBKDF2 accounts log in; a valid legacy account upgrades on its next login; new passwords fit the schema.
- Author CRUD works against the migrated schema.
- Failed or competing loan/return operations leave headers, details, status, and stock consistent.
- A normal user cannot access or execute employee administration; an admin can still manage employees safely.
- New book images survive Debug/Release changes and no longer depend on an open stream.
- Dashboard metrics are correct across years and quantities.
- `dotnet build`, automated tests, database postchecks, and the manual smoke checklist pass.

## Non-goals

This change does not replace WinForms, rewrite the project to EF Core, redesign visual styling, or automatically delete the existing backup folders. Package upgrades are constrained to compatibility fixes discovered during verification.
