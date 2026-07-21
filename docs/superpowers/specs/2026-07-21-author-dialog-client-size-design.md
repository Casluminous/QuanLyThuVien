# Author dialog client-area fix

## Problem

The author add/edit dialog sets its outer `Size` to 400 x 300 while its action buttons extend to Y=280. The Windows title bar and borders reduce the available client area, so the buttons are clipped.

## Design

- Set `ClientSize` to 400 x 300 instead of setting the outer `Size`.
- Keep all existing control positions, dimensions, styling, validation, and data operations unchanged.
- Limit the change to the author dialog.

## Verification

- Build the solution with a non-incremental compile.
- Confirm the dialog has at least 20 pixels of client space below the 35-pixel action buttons positioned at Y=245.

