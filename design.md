# Design — QuanLyThuVien

A locked design system for the Login and Dashboard redesign. Subsequent Hallmark work on these screens reads this file before emitting code. The system extends the existing Library Teal identity; it does not replace the visual language used by the remaining WinForms screens.

## Genre

Modern-minimal with a soft, welcoming tone. The interface should feel like a calm circulation desk: warm enough for a small library, exact enough for daily staff work.

## Macrostructure family

- Auth pages: **Warm Split** — a quiet identity pane beside a focused form; no photographic banner.
- App pages: **Operational Workbench** — greeting, KPI strip, task queue, recent activity, then analytics.
- Marketing pages: not applicable.
- Content pages: not applicable.

The Dashboard must remain task-first. Analytics support the work; they do not dominate the first viewport.

## Theme

- `--color-paper` `oklch(97.28% 0.0061 137.8)` — `#F4F7F3`
- `--color-paper-2` `oklch(95.22% 0.0125 164.8)` — `#E8F2ED`
- `--color-surface` `oklch(99.42% 0.0069 88.6)` — `#FFFDF8`
- `--color-ink` `oklch(28.79% 0.0317 185.7)` — `#17302D`
- `--color-ink-2` `oklch(52.69% 0.0257 182.5)` — `#5B706C`
- `--color-rule` `oklch(91.13% 0.0145 180.7)` — `#D8E5E2`
- `--color-accent` `oklch(51.09% 0.0861 186.4)` — Library Teal `#0F766E`
- `--color-focus` `oklch(70.38% 0.1230 182.5)` — `#14B8A6`
- `--color-warning` `oklch(66.58% 0.1574 58.3)` — `#D97706`
- `--color-danger` `oklch(57.71% 0.2152 27.3)` — `#DC2626`
- `--color-accent-ink` `oklch(99.42% 0.0069 88.6)` — warm surface text on teal fills

Teal is the single brand signal. Amber and red communicate operational status and must always appear with text, never as colour-only meaning. Warm paper and surface tokens are scoped to Login and Dashboard so the rest of the application is not unexpectedly recoloured.

## Typography

- Display: Segoe UI Variable Display when available, otherwise Segoe UI, weight 700, normal style.
- Body: Segoe UI, weight 400.
- Labels and metadata: Segoe UI Semibold, weight 600.
- Display tracking: compact by WinForms defaults; no simulated italic headings.
- Numeric KPI values use tabular alignment where the control supports it.
- Type scale: 9, 10, 11, 14, 20 and 28 pt. No additional size is introduced without updating this file.

No external font files are added. This preserves the existing deployment assumptions and the previously approved Segoe UI requirement.

## Spacing

Use a four-point base with an eight-point primary rhythm:

- `space-2xs`: 4 px
- `space-xs`: 8 px
- `space-sm`: 12 px
- `space-md`: 16 px
- `space-lg`: 24 px
- `space-xl`: 32 px
- `space-2xl`: 48 px

Sibling gaps use the scale. Raw offsets are limited to optical alignment inside custom-painted controls.

## Shape and depth

- Card radius: 14 px.
- Input and primary button radius: 10 px.
- Compact status radius: 999 px.
- Control height: minimum 44 px for primary interactive controls.
- Borders: one-pixel Library Teal rule.
- Shadow: one whisper shadow only; never stacked, coloured or glow-like.
- No thick side stripes, gradients, card-in-card nesting or decorative banner frames.

## Motion

- Motion stance: cut.
- Hover: one colour or one-pixel positional signal, never multiple simultaneous effects.
- Pressed: immediate darker state; no bounce.
- Focus: immediate high-contrast ring with no animation.
- Loading and error states do not move surrounding layout.
- No page-load reveals, chart entrance animation or decorative looping motion.

## Microinteractions stance

- Silent success for login navigation and dashboard loading.
- Login validation appears in a reserved one-line region beneath the fields.
- Keyboard focus always has an equivalent to hover.
- Tooltips are reserved for icon-only controls; visible labels need no tooltip.
- Errors name the failed task and the next action; no generic “Something went wrong.”

## CTA voice

- Primary CTA: filled Library Teal, 10 px radius, concise verb label such as `Đăng nhập`.
- Secondary CTA: transparent or warm-surface fill with a one-pixel rule.
- Icon-only close/password controls must have `AccessibleName` and a 44 px hit target.

## Per-page design

### Login

- Default client area: approximately `920×560`; minimum layout remains usable at `760×500`.
- Two-column split at normal widths: 42% identity pane and 58% form pane.
- Identity pane uses deep teal with the product name and one short welcome statement.
- Form pane uses warm paper, visible labels, two 44 px fields, stable helper/error space, show-password control and a full-width primary button.
- Remove the image banner and circular `PictureBox` login control.
- `Enter` submits, `Esc` closes and first focus goes to username.

### Dashboard

- Header: session-aware greeting on the left, current date on the right.
- KPI strip: open loans, catalog stock, readers and unpaid amount. Equal height; no per-card accent stripe.
- Work row: 60% `Cần xử lý hôm nay`, 40% `Phiếu gần đây`.
- Analytics row: borrowing trend receives roughly two-thirds width; category composition receives one-third.
- At narrow widths, layout becomes one column in the order KPI → attention → recent loans → trend → category.
- Dashboard retains both current charts and all current metrics. No database query or business rule is removed.

## What both pages must share

- Library Teal wordmark treatment.
- Warm paper and surface pair.
- Segoe UI type hierarchy.
- Primary button shape and focus treatment.
- One-pixel borders, 14 px cards and restrained depth.
- Concise Vietnamese labels and stable error regions.

## What may differ

- Login is deliberately quiet and split; Dashboard is information-dense and workbench-shaped.
- Login may use one large dark-teal identity pane. Dashboard may use dark teal only for compact emphasis, not as a large content card.
- Dashboard status colours may use warning and danger tokens with visible text labels.

## Accessibility

- All controls receive an explicit `AccessibleName` when the visible label is insufficient.
- Tab order follows the visual order.
- Focus is visible at all times for keyboard navigation.
- Error and status meaning is never colour-only.
- Text truncation uses ellipsis and retains the full value through an accessible name or tooltip when necessary.
- Login button is the form `AcceptButton`; Escape has an explicit close path.

## Exports

These exports document the system for reuse. The WinForms implementation maps them to `AppColors`, fonts and spacing constants rather than consuming CSS.

### tokens.css

```css
:root {
  --color-paper: oklch(97.28% 0.0061 137.8);
  --color-paper-2: oklch(95.22% 0.0125 164.8);
  --color-surface: oklch(99.42% 0.0069 88.6);
  --color-ink: oklch(28.79% 0.0317 185.7);
  --color-ink-2: oklch(52.69% 0.0257 182.5);
  --color-rule: oklch(91.13% 0.0145 180.7);
  --color-accent: oklch(51.09% 0.0861 186.4);
  --color-accent-ink: oklch(99.42% 0.0069 88.6);
  --color-focus: oklch(70.38% 0.1230 182.5);
  --color-warning: oklch(66.58% 0.1574 58.3);
  --color-danger: oklch(57.71% 0.2152 27.3);

  --font-display: "Segoe UI Variable Display", "Segoe UI", sans-serif;
  --font-body: "Segoe UI", sans-serif;

  --space-2xs: 0.25rem;
  --space-xs: 0.5rem;
  --space-sm: 0.75rem;
  --space-md: 1rem;
  --space-lg: 1.5rem;
  --space-xl: 2rem;
  --space-2xl: 3rem;

  --text-xs: 0.75rem;
  --text-sm: 0.875rem;
  --text-md: 1rem;
  --text-lg: 1.25rem;
  --text-display: 1.75rem;

  --ease-out: cubic-bezier(0.16, 1, 0.3, 1);
  --ease-in: cubic-bezier(0.7, 0, 0.84, 0);
  --ease-in-out: cubic-bezier(0.65, 0, 0.35, 1);
  --dur-micro: 120ms;
  --dur-short: 220ms;
  --dur-long: 420ms;

  --rule-fine: 1px;
  --radius-card: 14px;
  --radius-input: 10px;
  --radius-pill: 999px;
}
```

### Tailwind v4 `@theme`

```css
@theme {
  --color-paper: oklch(97.28% 0.0061 137.8);
  --color-paper-2: oklch(95.22% 0.0125 164.8);
  --color-surface: oklch(99.42% 0.0069 88.6);
  --color-ink: oklch(28.79% 0.0317 185.7);
  --color-ink-2: oklch(52.69% 0.0257 182.5);
  --color-rule: oklch(91.13% 0.0145 180.7);
  --color-accent: oklch(51.09% 0.0861 186.4);
  --color-focus: oklch(70.38% 0.1230 182.5);
  --font-display: "Segoe UI Variable Display", "Segoe UI", sans-serif;
  --font-body: "Segoe UI", sans-serif;
  --spacing-xs: 0.5rem;
  --spacing-sm: 0.75rem;
  --spacing-md: 1rem;
  --spacing-lg: 1.5rem;
  --radius-card: 14px;
  --radius-input: 10px;
  --ease-out: cubic-bezier(0.16, 1, 0.3, 1);
}
```

### DTCG `tokens.json`

```json
{
  "$schema": "https://design-tokens.github.io/community-group/format/",
  "color": {
    "paper": { "$value": "oklch(97.28% 0.0061 137.8)", "$type": "color" },
    "paper-2": { "$value": "oklch(95.22% 0.0125 164.8)", "$type": "color" },
    "surface": { "$value": "oklch(99.42% 0.0069 88.6)", "$type": "color" },
    "ink": { "$value": "oklch(28.79% 0.0317 185.7)", "$type": "color" },
    "ink-2": { "$value": "oklch(52.69% 0.0257 182.5)", "$type": "color" },
    "rule": { "$value": "oklch(91.13% 0.0145 180.7)", "$type": "color" },
    "accent": { "$value": "oklch(51.09% 0.0861 186.4)", "$type": "color" },
    "focus": { "$value": "oklch(70.38% 0.1230 182.5)", "$type": "color" }
  },
  "font": {
    "display": { "$value": "Segoe UI Variable Display, Segoe UI, sans-serif", "$type": "fontFamily" },
    "body": { "$value": "Segoe UI, sans-serif", "$type": "fontFamily" }
  },
  "space": {
    "xs": { "$value": "0.5rem", "$type": "dimension" },
    "sm": { "$value": "0.75rem", "$type": "dimension" },
    "md": { "$value": "1rem", "$type": "dimension" },
    "lg": { "$value": "1.5rem", "$type": "dimension" }
  },
  "duration": {
    "micro": { "$value": "120ms", "$type": "duration" },
    "short": { "$value": "220ms", "$type": "duration" },
    "long": { "$value": "420ms", "$type": "duration" }
  }
}
```

### shadcn/ui CSS variables

```css
:root {
  --background: 97.28% 0.0061 137.8;
  --foreground: 28.79% 0.0317 185.7;
  --card: 99.42% 0.0069 88.6;
  --card-foreground: 28.79% 0.0317 185.7;
  --primary: 51.09% 0.0861 186.4;
  --primary-foreground: 99.42% 0.0069 88.6;
  --secondary: 95.22% 0.0125 164.8;
  --secondary-foreground: 28.79% 0.0317 185.7;
  --muted: 91.13% 0.0145 180.7;
  --muted-foreground: 52.69% 0.0257 182.5;
  --destructive: 57.71% 0.2152 27.3;
  --destructive-foreground: 99.42% 0.0069 88.6;
  --border: 91.13% 0.0145 180.7;
  --input: 91.13% 0.0145 180.7;
  --ring: 70.38% 0.1230 182.5;
  --radius: 14px;
}
```
