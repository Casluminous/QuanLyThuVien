# Responsive book catalog grid

## Goal

Make the **Kho sách** catalog use the available window width. A wider window must show more books per row instead of remaining fixed at three cards.

## Selected behavior

- A `BookCardControl` keeps its current visual size of 240 x 340 pixels.
- The catalog calculates the number of columns from the current visible width of the catalog panel.
- Cards use a consistent 20-pixel gap and a 10-pixel outer padding.
- The grid recalculates after the catalog is populated and whenever the form or catalog panel changes size.
- A narrow window reduces the column count to at least one card. Additional rows remain available through the existing vertical scrollbar.
- The catalog does not stretch cards and does not introduce horizontal scrolling.

## Implementation boundary

`FormSach.LayoutCatalog` remains the single place responsible for catalog positioning. It will derive the column count from the panel client width, set each card's fixed size explicitly, then position each card in row-major order. No database, book-detail, image-storage, or table-view behavior changes.

## Failure handling

The layout calculation must tolerate a panel that is not yet fully measured by falling back to one column. It must never divide by zero or place a card outside the left padding.

## Verification

Build the WinForms solution, then open **Kho sách** and resize the application. Confirm that a normal-width window keeps the existing compact cards, a maximized window adds columns, and a narrow window returns to fewer columns without clipping or horizontal scrolling.
