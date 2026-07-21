# Library Teal theme for QuanLyThuVien

## Summary

Standardize the WinForms application around a light-only Library Teal theme. The redesign changes visual tokens, shared controls, page-level styling, responsive presentation, and small keyboard/accessibility affordances while preserving all data, permissions, validation, and borrowing/return workflows.

## Theme tokens

- Primary: `#0F766E`; primary dark: `#115E59`; primary light: `#CCFBF1`.
- Accent: `#D97706`; success: `#15803D`; warning: `#B45309`; danger: `#DC2626`; info: `#0369A1`.
- Content background: `#F6FAF9`; surface/card: `#FFFFFF`; border: `#D8E5E2`.
- Primary text: `#17302D`; secondary text: `#5B706C`; focus: `#14B8A6`; selected surface: `#D9F3EF`.
- Keep Segoe UI and use an 8px spacing rhythm. Titles are 18–20px, sections 13–14px, body 10–11px, and metadata 9px.

## Shared controls and pages

- Modern buttons use 12px radius, teal primary states, visible hover/pressed/focus states, and readable disabled states.
- Text boxes and combo boxes use 10px radius, white surfaces, teal focus borders, and readable placeholders.
- Data grids use teal headers, subtle zebra/hover/selected rows, stable action-column widths, and both scroll directions.
- Cards and stat panels use white surfaces, 14px radius, and subtle shadows without gradients or external UI libraries.
- Apply the same system to the main navigation, dashboard, reports, books/catalog, categories, readers, staff, loans, returns, dialogs, login, first-run setup, and crop-image UI.

## UX and accessibility

- Normalize TabIndex and AccessibleName values for interactive controls.
- Set dialog AcceptButton/CancelButton, support Enter to submit and Esc to cancel, and focus the first useful field.
- Add short tooltips where an icon or compact action label is ambiguous.
- Do not change database schema, queries, permissions, validation rules, or business workflows.

## Verification

- Build Release with `dotnet build --configuration Release`.
- Verify layout at 1280x780, 1000x600, and 900x600, including all dialogs and action columns.
- Verify keyboard focus/order, contrast, hover/selected/disabled states, and unchanged create/edit/delete plus borrow/return/fine-edit behavior.
