# Date Validation and Stock Clarity Design

## Context

`Sach.SoLuong` is the available quantity: it decreases when a loan is created and increases when the loan is returned. Reinterpreting it as total owned copies would require a schema and workflow change that is outside this maintenance fix.

## Decisions

1. Keep `SoLuong` as available quantity and label the edit field accordingly.
2. Do not allow a new loan with a future loan date. Enforce this in both the input dialog and `DataAccess.InsertPhieuMuonFull`.
3. Do not allow a due date earlier than the loan date in the data-access operation.
4. Do not allow a reader-card expiry date earlier than its issue date. Keep the two date pickers synchronized and validate before saving.

## Error handling and verification

The loan method returns its existing user-facing failure reason without opening a transaction for invalid dates. The reader form shows a validation message and remains open. A successful build verifies compilation; the manual smoke cases are a future-date loan and an expiry date before the issue date.
