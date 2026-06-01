# Progress — 004-selector-navigator-hr-false-pending

## Plan

- [x] Diagnose the reported symptom (TabBar shell, `Show() returned null`).
- [x] Trace the cascade: confirm `SelectorNavigator.Show()` always returns `null` and the
      Fix-4 `FrameView` success branch never covers a selector.
- [x] Confirm `TabBarNavigator` and `NavigationViewNavigator` both derive from
      `SelectorNavigator` (reproduces on both shells).
- [x] Write spec.
- [x] Add `IsNullShowResultExpected` virtual to `ControlNavigator`.
- [x] Handle the expected-null case in `ControlNavigator<TControl>.ExecuteRequestAsync`.
- [x] Override `IsNullShowResultExpected => true` in `SelectorNavigator`.
- [x] Add framework red/green test
      `Given_TabBar_HotReload.When_TabNavigated_Then_NoPendingFailedRequestRecorded`.
- [ ] Build `Uno.Extensions-packageonly.slnf` (Release) clean — needs Windows/full workload, see Verification.
- [ ] Run the Navigation HR runtime/UI tests — needs the runtime-test head.

## Changes

- `src/Uno.Extensions.Navigation.UI/Navigators/ControlNavigator.cs`
  - Added `protected virtual bool IsNullShowResultExpected => false;` on the non-generic
    `ControlNavigator` base.
  - In `ControlNavigator<TControl>.ExecuteRequestAsync`, after the existing `FrameView`
    success branch, added an `IsNullShowResultExpected` branch that clears any pending slot
    and returns `Route.Empty` without warning / without recording an HR retry.
- `src/Uno.Extensions.Navigation.UI/Navigators/SelectorNavigator.cs`
  - `protected override bool IsNullShowResultExpected => true;` with an explanatory comment.
- `src/Uno.Extensions.Navigation.UI.Tests/Given_TabBar_HotReload.cs`
  - New test `When_TabNavigated_Then_NoPendingFailedRequestRecorded` asserting the
    `TabBarNavigator` holds no phantom pending request after a successful tab navigation.

## Verification notes

- The fix is route-flow-neutral: the prior failure path already returned `Route.Empty`, so
  `CoreNavigateAsync.Trim` behaves identically. The only deltas are (a) suppressed false
  warning and (b) `ClearPendingFailedRequest()` instead of `RememberPendingFailedRequest()`.
  Existing `Given_TabBar_HotReload` tests assert content/VM values via the unchanged route
  flow and remain green.
- Runtime/UI HR tests require the WinUI runtime-test head (`stage-build-runtimetests-skia*`)
  and were not run in this environment; they must be run on a Windows/full-workload host
  before merge per AGENTS.md §5.

## Review

(to be completed after CI build + runtime tests)
