# Borrow, return, and staff guard fixes

## Goal

Prevent loans for expired reader cards, allow deletion of ordinary staff when only one administrator remains, and keep the return dialog open when its database transaction fails.

## Loan eligibility

- The new-loan reader list contains only readers whose `TrangThai` is active and whose `HanSuDung` is today or later.
- The data-access layer repeats the same eligibility test inside the transaction before inserting `PhieuMuon` or reducing stock. A reader who expires or is disabled after the dialog opened cannot receive a new loan.
- An ineligible reader causes the loan transaction to roll back and returns `false`; the form shows a specific warning instead of treating it as an out-of-stock error.

## Staff deletion guard

- The current signed-in user still cannot delete their own account.
- The last active administrator cannot be deleted.
- The administrator-count rule applies only when the selected employee is an active administrator. Ordinary staff remain deletable regardless of the number of active administrators.

## Return failure behavior

- `TraNhieuSach` remains the transactional source of truth.
- The form checks its Boolean result. On `false`, it leaves the return dialog open, shows an error, and does not reload the main grid.
- On success, it closes the dialog and reloads the list as today.

## Scope and verification

Only `DataAccess`, `FormPhieuMuon`, `FormPhieuTra`, and `FormNhanVien` change. No DevOps, CI, schema triggers, or new test project is introduced. Verify with a build and manual scenarios: an expired card is absent from the reader selector, an ordinary staff account can be deleted with one active admin, and a forced failed return does not close the dialog.
