# ThemeService Targets Its Own Root Element, Not the XamlRoot's Content

**Status:** Implemented (branch `dev/vs/3120-themeservice-root-element`)
**Issue:** [#3120](https://github.com/unoplatform/uno.extensions/issues/3120)
**Affects:** `Uno.Extensions.Core.UI`
**Files touched:**

- `src/Uno.Extensions.Core.UI/Toolkit/ThemeService.cs`
- `src/Uno.Extensions.Core.UI.Tests/Given_ThemeService.cs` *(new red/fix/green test)*

## Problem

`Uno.Extensions.Toolkit.ThemeService` (consumed via `UseThemeSwitching` / `IThemeService`,
registered as `ScopedThemeService`) applies the theme to **`RootElement.XamlRoot.Content`**
instead of to **`RootElement`** itself.

When an app's content is hosted under a `XamlRoot` it does not own — e.g. a secondary app
loaded into a collectible `AssemblyLoadContext` whose `Window.Content` is re-parented into a
host's single shared `XamlRoot` — `XamlRoot.Content` resolves to the **host's** root visual,
not the app's. Toggling the theme then re-themes the host while the hosted app does not change.

Standalone apps are unaffected because there `window.Content == XamlRoot.Content`, so the two
expressions point at the same element and the bug is invisible.

### Root cause — write target diverges from read/observe target

`ThemeService` is internally inconsistent about which element represents "the app root":

| Operation | Element used | Location |
| --- | --- | --- |
| Read current theme (`IsDark`) | `RootElement` | `ThemeService.cs:57` |
| Observe theme changes (`ActualThemeChanged`) | `RootElement` | `ThemeService.cs:76–78` |
| **Write theme (`RequestedTheme`)** | **`RootElement.XamlRoot.Content`** | `ThemeService.cs:136` |

`RootElement` is captured once from `window.Content` (the app's real root) in the constructor
(`ThemeService.cs:37`). The setter `InternalSetThemeOnUIThread` then writes to a *different*
element:

```csharp
private bool InternalSetThemeOnUIThread(AppTheme theme)
{
    var existingIsDark = IsDark;
    var rootElement = RootElement?.XamlRoot?.Content as FrameworkElement; // <-- wrong target
    if (rootElement is null)
    {
        return false;
    }

    rootElement.RequestedTheme = theme switch { ... };
    SaveDesiredTheme(theme);

    if (existingIsDark != IsDark)
    {
        ThemeChanged?.Invoke(this, theme);
    }
    return true;
}
```

`RootElement.XamlRoot.Content` is the **root visual of the XamlRoot**. Under a foreign/shared
XamlRoot that is the host's root, so:

1. The host re-themes; the hosted app does not.
2. `IsDark` reads the *unchanged* `RootElement.ActualTheme`, so `existingIsDark != IsDark` is
   `false` and `ThemeChanged` never fires — observers of the event are silently broken too.

The `XamlRoot.Content` indirection is original code; `git log` shows it was only ever moved
(file relocation `c384fc16d`) and touched by an unrelated OS-theme commit — it does not guard
any known case.

## Solution

Target the element the rest of the class already uses — `RootElement` — so read, write, and
change-observation all act on the same element, while **retaining the `XamlRoot` null-check** as
the "is the element realized yet" gate:

```csharp
// before
var rootElement = RootElement?.XamlRoot?.Content as FrameworkElement;
if (rootElement is null) { return false; }

// after
var rootElement = RootElement as FrameworkElement;
if (rootElement?.XamlRoot is null) { return false; }
```

`RequestedTheme` (`ElementTheme`) cascades down the visual subtree, so setting it on the app's
own root themes the entire app and nothing above it.

### Why this is correct in both topologies

- **Standalone app:** once realized, `RootElement` *is* `window.Content`, which *is*
  `XamlRoot.Content`. Identical behavior to today — no regression.
- **Hosted / ALC app:** the app root *has* a `XamlRoot` (the host's shared one), so the gate
  passes and the theme is applied to the app's own root and its subtree; the host (an ancestor of
  that root) is untouched. `IsDark` then observes the change, so `ThemeChanged` fires correctly.

### Why keep the `XamlRoot` gate (conservative choice)

The only behavioral change is **where** the theme is written when the element is realized;
the "not realized yet → return `false` → retry on `Loaded`" path (`InitializeAsync`,
`ThemeService.cs:201–213`) is preserved exactly. This keeps every existing
`Given_ThemeService` test — which exercises bare, unrealized elements with a null `XamlRoot` —
behaving identically (they still bail and never fire an event), so the fix is minimal-impact and
the regression surface is confined to the hosted/foreign-XamlRoot case.

## Scope / non-goals

- **`Uno.Toolkit.UI.SystemThemeHelper.SetApplicationTheme(XamlRoot?, ElementTheme)`** exhibits
  the same `root.Content.RequestedTheme` shape, but it only receives a `XamlRoot` (no element to
  scope to) and is **not** in this code path. It lives in a different repo and cannot be fixed
  the same way without a signature change. Out of scope here.
- No public API change. `IThemeService` surface, `UseThemeSwitching`, and `ScopedThemeService`
  registration are unchanged.

## Testing (red/fix/green)

Per repo policy the fix must ship with a test that fails on current `main` and passes after.

1. **Red:** with the element hosted under a *different* root (a parent `FrameworkElement` whose
   `RequestedTheme` differs from the captured `RootElement`), assert that
   `SetThemeAsync(AppTheme.Dark/Light)` sets `RequestedTheme` on the **captured root element**,
   not on the ancestor/XamlRoot content. On current code the assertion fails (theme lands on the
   ancestor).
2. **Fix:** apply the one-line target change.
3. **Green:** the same test passes; add a companion assertion that `ThemeChanged` fires when the
   effective theme flips.
4. Keep an existing-behavior test for the standalone case (`window.Content == XamlRoot.Content`)
   to prove no regression.

The test is added to `Given_ThemeService.cs` in `Uno.Extensions.Core.UI.Tests` — a
UI-host-requiring (`[RunsOnUIThread]`, `Uno.UI.RuntimeTests`) project, alongside the existing
`ThemeService` coverage. It loads a `host → app` tree into `UnitTestsUIContentHelper.CurrentTestWindow`
so the app root has a real `XamlRoot` whose `Content` is the host, then asserts `SetThemeAsync`
themes the app root (not the host) and that `ThemeChanged` fires. These tests run in the
runtime-test stages, not via `dotnet test`.

## Acceptance criteria

- [ ] `ThemeService` applies `RequestedTheme` to its own root element, not `XamlRoot.Content`.
- [ ] Standalone theme switching unchanged.
- [ ] `ThemeChanged` fires when the effective theme changes in the hosted case.
- [ ] Red/fix/green test committed alongside the fix, in the correct project type.
- [ ] Release build of `Uno.Extensions-packageonly.slnf` is warning-free (`TreatWarningsAsErrors`).
