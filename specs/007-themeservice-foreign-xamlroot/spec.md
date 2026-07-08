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

## Testing (red/fix/green) — verified on the Skia runtime-test head

Two guards were added to `Given_ThemeService.cs` (`Uno.Extensions.Core.UI.Tests`, a
`[RunsOnUIThread]` / `Uno.UI.RuntimeTests` project). Both load a `host → app` tree into
`UnitTestsUIContentHelper.CurrentTestWindow` so the app root has a real `XamlRoot` whose `Content`
is the **host**, reproducing the foreign/shared-XamlRoot topology. They were executed by building
`Uno.Extensions-runtimetests.slnf` and launching the `Uno.Extensions.RuntimeTests` desktop head
headless (`UNO_RUNTIME_TESTS_RUN_TESTS=Given_ThemeService`, NUnit output) — there is no
`dotnet test` entry point for `*.UI.Tests`.

1. `When_HostedUnderForeignXamlRoot_Then_ThemeAppliesToOwnRoot_NotHost` — asserts the **write
   target**: the host (XamlRoot owner) is NOT re-themed (`hostRoot.RequestedTheme == Default`) and
   the app's own element *is* written (`!= Default`).
2. `When_HostedUnderForeignXamlRoot_WithRealDispatcher_Then_AppSubtreeBecomesDark_HostStaysLight`
   — uses the **real** `Uno.Extensions.Dispatcher` (not the synchronous test fake) and asserts the
   effective outcome: the app subtree's `ActualTheme` settles on `Dark` while the host stays
   `Light`.

**Why two guards / why not assert `== Dark` in test 1:** `ThemeService` re-applies the theme from
its own `RootElement.ActualThemeChanged` handler. Under the *synchronous test dispatcher* in a live
visual tree, that feedback settles `RequestedTheme` to a value that isn't guaranteed to be `Dark`
(observed: `Light`). The stable invariant the fix actually changes is the write **target** (host vs
app), so test 1 asserts that; test 2 then confirms — with the real dispatcher — that the effective
theme really does land `Dark` on the app and not the host. This was an empirical finding: an
initial `== Dark` snapshot assertion failed on the fix for this reason.

**Verified results (Skia desktop head):**

- Fix applied → all 6 `Given_ThemeService` tests pass (4 pre-existing + 2 new).
- `main` write-target logic (`RootElement.XamlRoot.Content`) → both new tests **fail** (the host is
  re-themed / the app subtree doesn't become Dark), confirming a genuine red/fix/green.
- The 4 pre-existing tests (bare, unrealized elements, null `XamlRoot`) are unchanged on both.

## Acceptance criteria

- [x] `ThemeService` applies `RequestedTheme` to its own root element, not `XamlRoot.Content`.
- [x] Standalone theme switching unchanged (4 pre-existing tests green; bare-element gate path identical).
- [x] Effective theme lands on the app (not the host) in the hosted case — verified with the real dispatcher.
- [x] Red/fix/green tests committed alongside the fix, in the correct project type, executed on the Skia head.
- [ ] Release build of `Uno.Extensions-packageonly.slnf` warning-free (`TreatWarningsAsErrors`) — runs in package CI.

## End-to-end (Studio ALC host) — not run in this environment

A full repro inside the real ALC host would add only assembly-load-context isolation on top of the
shared-XamlRoot topology already exercised by test 2 (the fixed code path — which element receives
`RequestedTheme` — is identical in both). It was **not** run here because it requires Uno Platform
licensing auth and the `uno-app` automation tooling, and the in-repo sample has no theme toggle.
The real-dispatcher guard (test 2) is the behavioral equivalent at the unit level.
