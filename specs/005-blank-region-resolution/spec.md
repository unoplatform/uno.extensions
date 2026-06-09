# 005 — Blank screen: NavigationRegion permanently orphans itself on transient detachment

## Problem

On WebAssembly (frequently on optimized/release builds, rarely on debug), an Uno.Extensions app can render a
**completely blank screen**. A feedback bundle captured the smoking gun — the generated app logged, four times,
during a hot-reload cycle right before going blank:

```
Uno.Extensions.Navigation.Regions.NavigationRegion:
AssignParent - (Name: ) Unable to find service provider for root navigator
```

## Root cause

`NavigationRegion`s created from XAML via `uen:Region.Attached="True"` (the structural `Grid`/`TabBar` regions in a
typical `MainPage`) resolve their place in the navigation tree in `AssignParent()`. They walk the **visual
ancestry**:

1. `FindParentRegion` — looks for an ancestor carrying a `Region.Instance` (a parent region).
2. failing that, `FindServiceProvider` — looks for an ancestor carrying the root `Region.ServiceProvider`, which is
   attached exactly once, to the root `ContentControl`, in `FrameworkElementExtensions.HostAsync`.

During async visual-tree construction and **hot-reload view-swaps** (Uno's HR processor creates a fresh instance
and grafts the new subtree; its `Loaded` fires *before* `RestoreState`), the new subtree's `Loaded` can fire while
its ancestry up to the navigation root is **transiently not connected**. Both lookups return `null`. `AssignParent`
logs and returns; `HandleLoading` then runs `HandleLoaded`, which **commits `_isLoaded = true` and unsubscribes
from `Loading`/`Loaded`**. There is **no retry**. The region is permanently orphaned — no parent, no services,
`Navigator()` (which resolves through `Services`) returns null — so it never hosts navigation and the screen stays
blank.

`HandleLoading` is reached from `ViewLoaded`, `ViewLoading`, **and the constructor** (when the element is already
loaded at attach time, common during an HR re-graft), so the trap is not limited to the event path.

The defect is **build-independent logic**: `HandleLoaded` conflates "loaded" with "resolved" and commits
irrevocably on "loaded" even when resolution failed. Release only raises the *probability* the race fires (tighter
layout/HR window); debug usually has enough dispatcher idle time for the tree to be connected by `Loaded`.

## Fix

Make resolution **non-terminal**. A region is "done" only when it is loaded **and** (rooted OR has a parent).
While loaded-but-unresolved, re-attempt **driven by the `LayoutUpdated` event** (not a dispatcher poll) until it
resolves; on a generous runaway cap give up loudly once. Implementation lives in `Regions/NavigationRegion.cs`
(+ a one-word visibility change in `FrameworkElementExtensions.cs` to reuse the existing dispatcher accessor for a
single safe deferred-commit hop). See `progress.md` for the step list.

### Why event-driven (`LayoutUpdated`), not a source-side ordering fix and not a poll

At initial app launch the root `Region.ServiceProvider` is attached before content navigation, so ordering is
already correct there. The blank happens on **HR re-graft**, where the root anchor already exists but the new
subtree is momentarily disconnected from it. Two recovery signals cover all cases:

- **Re-parent** (subtree moved under the real root) already fires `Unloaded`→`Loaded`, which re-drives
  `AssignParent`. The fix simply **stops unsubscribing `Loaded`/`Loading` on the failed path**, so this recovers
  for free.
- **In-place linkage completes during a layout pass** (the HR re-graft case: the new instance fires `Loaded` once
  before its ancestry is connected and never re-cycles). The framework signal for "the element has settled in the
  tree" is **`LayoutUpdated`** — which `FrameworkElementExtensions.EnsureElementLoaded` already hooks alongside
  `Loaded`/`SizeChanged` for exactly this reason (*"sometimes Loaded is never fired … not always in right order"*).
  `LayoutUpdated` is **quiet at idle** (fires only when layout actually runs), so re-checking on it is event-driven,
  not a busy-spin. We unsubscribe the instant resolution succeeds.

The only dispatcher use is a *single* hop to defer the commit safely off the `LayoutUpdated` callback (avoiding tree
mutation mid-layout — the repo's HR cascade and `EnsureLoaded` Android-yield show the same caution). It is not a
retry loop.

### Edge cases handled

- **Re-entrancy**: `ResolveRetry` abandons if a different path (`ReassignParent`, HR cascade) already resolved the
  region, so it never clobbers a resolution or fires a spurious re-cascade.
- **Constructor-already-loaded path**: recovery is anchored in `HandleLoaded`/the predicate, not in event
  subscriptions, so it covers the path where `Loading`/`Loaded` were never subscribed.
- **Post-unload**: `_services` is not cleared by `ViewUnloaded`, and it is part of the unresolved predicate, so a
  stale retry no-ops; the attempt budget is reset on unload so a later re-orphan gets a fresh allowance.
- **HR re-cascade**: a successful retry funnels back through the full `HandleLoaded`, preserving the
  `_wasUnloaded`/`_replacedByHotReload` re-cascade semantics.
- **Log spam**: per-attempt message demoted to Trace; a single Warning (with a compact ancestor chain) is emitted
  only when the bounded attempts are exhausted (a genuine structural detachment).

### Residual (pre-existing, not introduced)

`AssignParent` can let a region self-elect as root if `FindServiceProvider` succeeds before a parent
`Region.Instance` is reachable. In the real scenario the root `Region.Instance` and `Region.ServiceProvider` are
co-located on the same `ContentControl`, and `FindParentRegion` runs first, so a structural region cannot wrongly
self-elect. Left as-is for this change.

## Tests

- `Given_RegionResolutionResilience` (runtime/UI test): deterministically loads a `Region.Attached` view with no
  reachable navigation root (orphans on current code), then wires the root and asserts the region recovers a
  navigator. **RED on `main`, GREEN with the fix** — independent of build flavor.
- Regression: existing `Given_NavigatorStartup`, `Given_HotReload*`, `Given_TabBar_HotReload`.

## Verification

Deterministic runtime test (primary, Debug) + a real Release-WASM app run confirming the blank and the
`"Unable to find service provider for root navigator"` warning are gone. See `progress.md`.
