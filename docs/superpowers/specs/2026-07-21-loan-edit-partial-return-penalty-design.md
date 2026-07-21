# Loan editing, partial return, and editable penalty design

## Context

The application stores available stock in `Sach.SoLuong`. A loan is represented by one `PhieuMuon` header and one `ChiTietPhieuMuon` row for each book title. Each detail has one quantity, one optional return date, and one penalty value. This model can represent returning selected book titles, but it cannot represent returning only some copies from a single detail row.

The current create-loan transaction protects stock from concurrent over-borrowing, but the UI permits unrelated grid cells to be edited and the data-access boundary does not validate every invariant. The current return transaction always returns every outstanding detail and always closes the loan.

## Scope

- Allow full editing of a loan before any detail has been returned.
- Allow returning one or more selected book-title rows.
- Let staff override the suggested total penalty before confirming a return.
- Let an active Admin correct the total penalty after a loan is fully returned.
- Repair the related date, validation, stock, and status inconsistencies.
- Preserve the existing WinForms, `System.Data.SqlClient`, static `DataAccess`, and database tables.

This change does not introduce a return-batch table, audit log, Entity Framework, or partial-copy returns within one book-title row.

## Business rules

### Loan editing

1. A loan may be fully edited only while its status is `Đang mượn` and every detail has `NgayTra IS NULL`.
2. Editable fields are reader, loan date, due date, selected book titles, and quantity for each selected title.
3. Once any detail has been returned, header and loan-detail editing is locked. Remaining titles can only proceed through the return flow.
4. A proposed loan must contain at least one unique book ID and every quantity must be positive.
5. The loan date cannot be in the future and the due date cannot be before the loan date.
6. The selected reader must still be active and unexpired when the transaction commits.
7. Internal IDs, names, and displayed available stock are read-only in the UI.

### Partial return

1. Staff can select one or more unreturned detail rows from an open loan.
2. Selecting a row returns its full `SoLuong`; returning only some copies from that row is outside this design.
3. The transaction reads the authoritative quantity from the database. The UI never supplies the quantity used to restore stock.
4. A detail can be returned only once. A stale or duplicate return causes the whole operation to roll back with a conflict message.
5. The loan changes to `Đã trả` only when no detail remains with `NgayTra IS NULL`; otherwise it stays `Đang mượn` and is displayed as partially returned.

### Penalty handling

1. The suggested total is `max(0, return date - due date) * 10,000 VND` for the current return action.
2. Staff can replace the suggestion with any nonnegative whole-VND amount before confirmation.
3. The confirmed total is distributed across the selected returned details using whole VND. Equal base amounts are assigned first and the final selected detail receives the remainder, so the stored sum equals the entered total exactly.
4. If a loan is returned in multiple actions, its total penalty is the sum of the confirmed amounts from those actions.
5. After a loan is fully returned, only an active Admin can replace the total penalty for the whole loan. The corrected total is redistributed across every returned detail without changing return dates or stock.

## Data-access operations

### Create loan

`InsertPhieuMuonFull` validates the request before opening a connection and repeats reader and stock checks in its transaction. It rejects empty, duplicate, zero, and negative detail input. The unsafe header-only `InsertPhieuMuon` path is removed because it can create an incomplete loan.

### Update loan

A new transactional operation receives the loan ID, updated header fields, and the complete proposed detail list.

1. Lock the loan header and current details with `UPDLOCK, HOLDLOCK`.
2. Reject a missing, completed, or partially returned loan.
3. Validate the reader inside the same transaction.
4. Lock the union of old and proposed book rows.
5. For every affected book, calculate `new available = current available + old loan quantity - proposed loan quantity` and reject a negative result.
6. Update stock, replace the detail rows, update the header, and commit once.

This delta calculation preserves unrelated stock changes and makes the edit atomic.

### Return selected titles

A replacement for `TraNhieuSach` receives only the loan ID, unique selected book IDs, and confirmed total penalty.

1. Validate a nonempty unique selection and a nonnegative whole-VND penalty.
2. Lock the loan, selected unreturned details, and their book rows.
3. Require every selected ID to belong to the loan and still be unreturned.
4. Read each quantity from the locked detail, write its return date and allocated penalty, and add that quantity back to stock.
5. Query for remaining unreturned details and update the header status accordingly.
6. Commit once; any mismatch rolls back everything.

The unused single-title `TraSach` and arbitrary `CapNhatTrangThaiPhieuMuon` methods are removed to prevent inconsistent stock or status paths.

### Correct completed-loan penalty

A separate transaction receives the loan ID, corrected total, and acting employee ID. It verifies that the employee is active and has role `Admin`, verifies that the loan is fully returned, then redistributes the total across all returned details. No stock, date, reader, or status value changes.

## User interface

### Loan management

- Add `Sửa` and Admin-only `Sửa tiền phạt` action columns beside `Chi tiết`.
- Display derived states `Đang mượn`, `Quá hạn`, `Đã trả một phần`, and `Đã trả` without persisting `Quá hạn`.
- Compare due dates with `DateTime.Today`, not `DateTime.Now`, so a loan is not overdue during its due date.
- Reuse the create dialog for editing. Only selection and quantity cells are editable; quantity input is validated before submission.
- Hide or disable `Sửa` once any detail has been returned.

### Return screen

- The main screen continues to list open loans with at least one unreturned detail.
- The return dialog shows all details, includes a checkbox for each unreturned row, and disables already-returned rows.
- A numeric whole-VND input displays the suggested total and remains editable.
- Confirmation requires at least one selected row and keeps the dialog open on validation, conflict, or database failure.
- Dialogs use `ClientSize` so action buttons are not clipped by window chrome.

## Schema consistency

The new-install `database.sql` gains the same nonnegative stock, nonnegative price, positive loan quantity, and nonnegative penalty constraints already present in the existing migration. No new table or column is required for this feature. The existing `QuocTia`/`QuocTich` mismatch is a separate defect and is not modified as part of the loan/return implementation.

## Error handling

- Validation failures return specific user-facing messages without starting a transaction when possible.
- Concurrent changes return a conflict result rather than displaying success.
- SQL exceptions are caught at the form boundary, the dialog stays open, and stock/status changes remain rolled back.
- Raw formatted UI text is never parsed back into a penalty value.

## Verification

1. Build the project with zero errors; use a separate output directory if the running application locks the default output.
2. Verify create-loan rejection for empty, duplicate, zero, negative, invalid-date, expired-reader, and insufficient-stock requests.
3. Edit an untouched open loan by changing reader, dates, selected titles, and quantities; verify exact stock deltas.
4. Attempt to edit a partially returned and a completed loan; both must be rejected.
5. Return one selected title and verify its stock, date, penalty, and the still-open header.
6. Return the remaining titles and verify the header becomes `Đã trả`.
7. Simulate a duplicate/stale return and verify the transaction rolls back without double-incrementing stock.
8. Override the suggested penalty and verify the stored detail sum equals the entered total.
9. Verify a normal employee cannot correct a completed-loan penalty and an active Admin can.
10. Verify a due-today loan is not displayed or counted as overdue.
11. Run `git diff --check` and confirm the existing uncommitted author and publisher dialog fixes remain intact.

