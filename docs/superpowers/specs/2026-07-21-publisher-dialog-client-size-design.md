# Publisher dialog client-area fix

## Problem

The publisher add/edit dialog sets its outer `Size` to 400 x 300. Window chrome reduces the client area and clips the action buttons at the bottom.

## Design

- Set `ClientSize` to 400 x 300 instead of setting the outer `Size`.
- Keep control positions, styling, validation, and database behavior unchanged.
- Limit the code change to the publisher dialog.

## Verification

- Build the project to a separate output directory if the running application locks the default output.
- Confirm the 35-pixel buttons at Y=225 fit inside the 300-pixel client area with 40 pixels below them.

