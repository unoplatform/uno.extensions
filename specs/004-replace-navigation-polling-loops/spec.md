# Replace Polling Loops in Navigation Initialization

**Status:** Proposed
**Affects:** `Uno.Extensions.Navigation.UI`
**Files in scope:**

- `src/Uno.Extensions.Navigation.UI/Navigator.cs` — `EnsureChildRegionsAreLoaded()`
- `src/Uno.Extensions.Navigation.UI/Navigators/SelectorNavigator.cs` — `DeferredInitialSelectionCheckAsync()`

## Problem

Two polling loops were introduced into the navigation startup path to paper over timing races between visual-tree events and the navigation cascade. Both loops are correctness-by-iteration: they keep yielding (or sleeping) until a piece of UI state that *should* have been signalled by an event happens to be observable. They mask the real race instead of resolving it, and they make the navigation engine's behaviour non-deterministic in a way that's hard to test or reason about.

Both loops need to be replaced with event-driven or completion-source-based logic. This spec captures what they do today, why they were added, what's wrong with them, and the shape of an acceptable replacement.

## Loop 1 — `EnsureChildRegionsAreLoaded()` in `Navigator.cs`

### Current code

`src/Uno.Extensions.Navigation.UI/Navigator.cs:899-928`:

```csharp
private async Task EnsureChildRegionsAreLoaded()
{
    var loadId = Guid.NewGuid();
    PerformanceTimer.Start(Logger, LogLevel.Trace, loadId);
    // This is required to ensure nested elements (eg Content in a ContentControl)
    // are loaded. This will ensure the Children collection is correctly populated
    await CheckLoadedAsync();

    // Child NavigationRegions attach themselves to parent.Children during their
    // Loaded event handler (HandleLoading → AssignParent). In some hosting
    // scenarios (e.g., in-process AssemblyLoadContext loading), these Loaded
    // events may be queued on the dispatcher but not yet processed when this
    // method returns. Re-schedule on the dispatcher to ensure its queue is
    // drained and pending attachments complete before the caller checks
    // Region.Children.
    if (Region.Children.Count == 0 && Region.View?.IsLoaded == true)
    {
        const int maxAttempts = 5;
        for (var attempt = 0; attempt < maxAttempts && Region.Children.Count == 0; attempt++)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTraceMessage($"Children not yet attached after view loaded (attempt {attempt + 1}/{maxAttempts}), re-scheduling on dispatcher");
            }
            await Dispatcher.ExecuteAsync(async ct => true, CancellationToken.None);
        }
    }

    PerformanceTimer.Stop(Logger, LogLevel.Trace, loadId);
}
```

### Why it was added

Commit **`04baec63b`** ("fix: yield to dispatcher in EnsureChildRegionsAreLoaded when children not yet attached", 2026-05-11):

> In some hosting scenarios (e.g., in-process AssemblyLoadContext loading used by Studio Live), child NavigationRegion `Loaded` events may be queued on the dispatcher but not yet processed when `CheckLoadedAsync` returns. This causes `CoreNavigateAsync` to see `Region.Children.Count == 0` and fail with 'has no children to forward request to' or 'Show() returned null'.
>
> After `CheckLoadedAsync`, if children are still empty despite the view being loaded, yield to the dispatcher up to 5 times via `Task.Yield()` to allow pending region attachments to complete. This is zero-cost in normal scenarios where children attach synchronously.

### The underlying race

Child `NavigationRegion` instances attach themselves to `parent.Children` from their own `Loaded` event handler (`HandleLoading → AssignParent`). `CheckLoadedAsync` awaits `view.EnsureLoaded(timeoutInSeconds: 5)` on the **parent** view — which fires the parent's `Loaded` event and returns. But the children's `Loaded` events are independent dispatcher continuations that may not have run yet. The navigation cascade then sees `Region.Children.Count == 0` and concludes the region has no nested routes to dispatch into.

In a normal app the children's `Loaded` handlers run synchronously on the same dispatcher tick — `Region.Children` is populated by the time `await CheckLoadedAsync()` returns. In ALC-hosted scenarios (Studio Live) the dispatcher queue has more pressure and the children's `Loaded` continuations land on a later tick.

### What's wrong with the current fix

1. **It races against the wrong event.** The yield loop has no relationship to "child Loaded fired" — it just hopes the dispatcher has drained enough work after N pumps. With heavy dispatcher pressure (e.g. a hot-reload delta firing dozens of property-change notifications), 5 yields can finish before the children's `Loaded` handlers run, and the loop still exits with `Children.Count == 0`.
2. **The "fix" trips during normal startup too.** The loop only runs when `Region.Children.Count == 0 && Region.View?.IsLoaded == true`. That's also the legitimate state for a leaf region with no children. The loop has no way to distinguish "no children expected here" from "children are about to attach", so it pays the yield cost on every leaf navigation.
3. **`Dispatcher.ExecuteAsync(async ct => true, …)` is a dispatcher round-trip, not a guarantee.** It yields once. Five round-trips ≠ "the dispatcher queue is empty" — Uno's `IDispatcher` does not expose drain semantics.
4. **No telemetry on the failure mode.** If the loop exits with `Children.Count == 0`, the downstream warn in `CoreNavigateAsync` ("Region has no children…") doesn't say "we waited 5 dispatcher rounds for them" — the symptom looks identical to "this region has no children by design".
5. **Hard-coded `maxAttempts = 5`.** Picking the number was guesswork. There is no test that demonstrates 5 is the right floor; there is no machine where the loop is provably sufficient.

### Replacement direction

Make the wait explicit and event-driven, not iterative:

- **Subscribe to the attach event, don't poll for the result.** `NavigationRegion.HandleLoading → AssignParent` is the event we care about. Surface a `Children.Added` notification (or expose `ChildAttached` directly from `IRegion`) so `EnsureChildRegionsAreLoaded` can `await` the first child attachment with a bounded timeout, instead of yielding and re-checking the collection.
- **Bound the wait by an event-completion source.** Replace the `for (attempt = 0..5)` loop with a `TaskCompletionSource<bool>` that completes when either (a) at least one child has attached, or (b) `CheckLoadedAsync`'s timeout has elapsed. That makes "we waited and no child appeared" a single observable outcome.
- **Scope the wait to navigators that actually expect children.** Most regions don't have nested routes. The wait should be conditional on the resolver reporting that the current route has `Nested` entries (i.e. the cascade is *about* to descend into children), not on the generic empty-collection check.
- **Centralize the "view + children loaded" gate.** `CheckLoadedAsync`'s contract is "view is loaded". A separate `EnsureChildrenAttachedAsync` (or a strengthened `CheckLoadedAsync` override on container navigators like `ContentControlNavigator`, `FrameNavigator`, `PanelVisiblityNavigator`) keeps the responsibility on the navigator that knows whether children are expected.

Acceptance criteria for the replacement:

- No `for/while` loop, no `Task.Delay`, no repeated dispatcher round-trips.
- A test that exercises the ALC-hosted scenario (deferred `Loaded` events) passes without the new code knowing anything about ALCs or dispatcher pressure.
- The wait has a single timeout knob; default value documented and justified, not picked by trial-and-error.
- If the wait times out, the warning message says so explicitly and includes the expected-children count from the resolver.

## Loop 2 — `DeferredInitialSelectionCheckAsync()` in `SelectorNavigator.cs`

### Current code

`src/Uno.Extensions.Navigation.UI/Navigators/SelectorNavigator.cs:42-87`:

```csharp
private async Task DeferredInitialSelectionCheckAsync()
{
    // Yield to the dispatcher. On normal first load, the route cascade calls
    // Show() in the same dispatch cycle, so _showCalled is already true by
    // now. On XAML HR — and on initial load when the selector's containers
    // haven't materialised yet — _showCalled stays false and the framework
    // is responsible for kicking the initial selection itself.
    //
    // The selector may not have populated SelectedItem yet when this check
    // first runs (e.g. TabBar's default IsSelectable item is chosen on a
    // later layout pass than its Loaded event). Poll a few times with a
    // brief delay before giving up; if the regular route cascade arrives
    // first it flips _showCalled and we stop early.
    const int maxAttempts = 20;
    for (var attempt = 0; attempt < maxAttempts; attempt++)
    {
        var done = await Dispatcher.ExecuteAsync(async ct =>
        {
            if (_showCalled || Region.View is not FrameworkElement view)
            {
                return true;
            }

            var selected = SelectedItem;
            if (selected is null)
            {
                return false;
            }

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebugMessage($"Triggering navigation for missed initial selection (XAML HR)");
            }

            await SelectionChanged(view, selected);
            return true;
        }, CancellationToken.None);

        if (done)
        {
            return;
        }

        await Task.Delay(50);
    }
}
```

### Why it was added

Two commits, growing from a single yield into a 20-attempt poll:

1. Commit **`ec4d6bfdc`** ("fix: Trigger deferred initial selection in SelectorNavigator after XAML HR", 2026-04-24):

   > During XAML HR page replacement, the selector control (`TabBar`/`NavigationView`) fires `SelectionChanged` before the `SelectorNavigator` is created, so the event is lost and content stays blank. Add a deferred check in `ControlInitialize` that yields one dispatch cycle. On normal first load, `Show()` is called by the route cascade before the deferred check runs (no-op). On XAML HR, `Show()` is never called, so the check triggers navigation for the existing selection.
   >
   > Fixes <https://github.com/unoplatform/uno.extensions/issues/#2971>

2. Commit **`926bdfa85`** ("feat: enhance navigation handlers for improved hot-reload support", 2026-05-15) — same PR as the `NavigationRouteContext.Resolver` fix in spec [003-hot-reload-navigation-route-resolver](../003-hot-reload-navigation-route-resolver/spec.md):

   The original "yield once" turned out to be insufficient because the selector populates `SelectedItem` on a later layout pass than its `Loaded` event. The single yield was promoted to the current 20 × 50ms poll with a comment explaining that the selector's default item may not be chosen until a measure/arrange pass that hasn't run yet.

### The underlying races

The method is trying to compensate for **two** distinct races at once:

- **Race A (XAML HR):** The selector control's `SelectionChanged` fires before the `SelectorNavigator` is created (because XAML HR replaces the visual tree and the previously-attached selection change event has nowhere to fire to). When the new navigator is constructed, the selection event has already happened and is lost. The navigator needs to *retroactively* invoke its own selection-handling path on the item that's already selected.
- **Race B (initial load):** On a cold start, the selector's `SelectedItem` is `null` when `ControlInitialize` runs because the items source has not been materialised yet. The default-selectable item is chosen on a later layout pass. The navigator needs to wait for `SelectedItem` to be populated before deciding whether Race A applies.

The 20-attempt poll exists because (a) the method can't distinguish A from B until `SelectedItem` becomes non-null, and (b) "non-null SelectedItem" is the only signal the current code has for "the selector has finished its initial layout".

### What's wrong with the current fix

1. **`Task.Delay(50) × 20 = 1 second of wall-clock latency before the navigator gives up.** On every selector creation. On every cold start. Most apps never hit Race A at all and still pay the latency budget when `_showCalled` flips quickly (the loop exits on the first iteration). But if `_showCalled` *doesn't* flip and `SelectedItem` *is* null on every iteration — the legitimate "selector genuinely has no default item" case — the navigator spends a full second polling before exiting.
2. **The fast-exit path is fast only because of the dispatcher round-trip.** Each iteration does `Dispatcher.ExecuteAsync(...)` first, then `Task.Delay`. The dispatcher hop is the only thing that gives the route cascade a chance to call `Show()` and set `_showCalled = true`. Removing the loop without preserving that yield would break the normal-load no-op behaviour.
3. **`Task.Delay` is a wall-clock wait, unrelated to layout passes.** `SelectedItem` populates when the selector finishes a measure/arrange cycle. There's no relationship between "50ms elapsed on the system clock" and "the layout pass has run". On a slow CI machine or under dispatcher pressure, 50ms may not be a full layout cycle; on a fast machine it may be 5× longer than needed.
4. **Hard-coded `maxAttempts = 20`.** Like loop 1, the number was picked by trial. No principled value.
5. **Two unrelated races, one mechanism.** The 20-attempt poll is doing double duty for "wait for the selector's first layout pass" (Race B) and "the navigator was constructed after the selection event fired" (Race A). The right fixes for those races are different (`Loaded` + items-source completion vs. inspecting current `SelectedItem` once). Conflating them in one loop means the fix can't be tested for one race without exercising the other.
6. **Silent timeout.** After 20 attempts, the method returns. The caller (`ControlInitialize`) has no idea whether the deferred check fired, timed out, or short-circuited. If a user opens a `TabBar` and the content never appears, there is no log entry distinguishing "no default item was ever selected" from "we waited 1 second and then gave up".

### Replacement direction

Split the two races and fix each with the event it's actually waiting on:

- **Race B (wait for the selector's first layout pass):** Subscribe to `SizeChanged`, `LayoutUpdated`, or — better — `ItemsSourceChanged`/`Items.VectorChanged` on the underlying control, plus an `IsLoaded`-becomes-true check, and complete a `TaskCompletionSource<bool>` from any of them. When the source completes, inspect `SelectedItem` exactly once. No polling, no `Task.Delay`.
- **Race A (selector constructed after selection event):** Move the check inline into `ControlInitialize` after Race B's `TaskCompletionSource` completes. If `_showCalled` is still `false` and `SelectedItem` is non-null at that moment, fire `SelectionChanged` once. The `_showCalled` flag stays as the gate — it's already correct, it just shouldn't be polled.
- **Eliminate `Task.Delay` entirely.** The replacement should have no wall-clock waits. The timeout (if needed at all) should be a single `CancellationTokenSource(TimeSpan.FromSeconds(N))` on the `TaskCompletionSource`, with a documented default.
- **Tighten the gate.** "On normal first load the route cascade calls Show() in the same dispatch cycle" is currently a *hope*, not a guarantee. The new code should treat the cascade as the primary path and the deferred check as a true fallback — i.e., the deferred check awaits the cascade-vs-layout race and acts only when the cascade has definitively *not* fired by the time the layout settles. A small `await Task.Yield()` after `ControlInitialize` is acceptable; an iteration is not.

Acceptance criteria for the replacement:

- No `for/while` loop, no `Task.Delay`.
- Race A (XAML HR with already-selected item) covered by a focused unit test.
- Race B (cold start with deferred default selection) covered by a focused unit test.
- Worst-case latency on cold start: one `await Task.Yield()` plus, if the cascade still hasn't fired, the time to a single `Loaded`/`LayoutUpdated` event. No `1000ms` ceiling.
- A diagnostic log fires exactly once per `ControlInitialize` describing the outcome: "cascade fired", "deferred check triggered selection", or "deferred check timed out waiting for SelectedItem".

## Cross-cutting concerns

Both loops share the same anti-patterns and the replacements should be cross-checked against each other:

1. **No polling primitives in the navigation engine.** After this change, the navigation engine should not contain any `for`-with-`Task.Delay` or `Dispatcher.ExecuteAsync` repeated-yield loops. If a future race emerges, the answer is "wait on the right event", not "yield N times and hope".
2. **One timeout knob per wait.** Each event-driven wait gets a single, documented timeout. No more `maxAttempts × delay` arithmetic.
3. **Failure paths log distinctly.** A wait timing out should be observably different from a wait completing — the warning message in `CoreNavigateAsync` ("Region has no children…") currently subsumes both outcomes, which is the root cause of why the loops were added in the first place (they masked the diagnostic instead of producing one).
4. **Tests live in `Uno.Extensions.Navigation.UI.Tests`** (UI-host-requiring), not in `Uno.Extensions.Navigation.Tests`. The races are visual-tree-driven; a non-UI test cannot reproduce them.

## Out of scope

- The `view.EnsureLoaded(timeoutInSeconds: 5)` call inside the default `CheckLoadedAsync` (line 976) is a different mechanism — it's an event-driven wait against `FrameworkElement.Loaded` with a bounded timeout, not a polling loop. It is not in scope here.
- The hot-reload route-retry walk added in [003-hot-reload-navigation-route-resolver](../003-hot-reload-navigation-route-resolver/spec.md) is also event-driven (it fires off the `[MetadataUpdateHandler]` callback path) and is not in scope.
- `SelectorNavigator.RegionCanNavigate`'s lazy route registration (`EnsureRouteRegistered`) is unrelated to the polling loops and stays as-is.

## References

- Commit `04baec63b` — origin of loop 1.
- Commit `ec4d6bfdc` — origin of loop 2 (single-yield form).
- Commit `926bdfa85` — promoted loop 2 to a 20-attempt poll.
- Upstream issue [unoplatform/uno.extensions#2971](https://github.com/unoplatform/uno.extensions/issues/2971) — Race A on `SelectorNavigator`.
- Spec [003-hot-reload-navigation-route-resolver](../003-hot-reload-navigation-route-resolver/spec.md) — companion fix for the broader hot-reload navigation initialization issue.
