# Progress — FeedView mock sources

Branch: `dev/nr/feedview-mock-source`

- [x] Spec written (`spec.md`)
- [x] Core: `Feed.Value<T>` public factory over `ValueFeed<T>` (`Core/Feed.cs`)
- [x] Core: `MockFeed` (envelope detection, dictionary + reflection readers, refresh handling) (`Sources/MockFeed.cs`, `Sources/MockFeedException.cs`)
- [x] UI: `FeedView` source coercion (cached `_coercedSource` field, `OnSourceChanged`/`Enable`/`OnApplyTemplate`)
- [x] Unit tests: `Given_MockFeed` — 22 cases (package CI surface), all passing
- [x] Unit tests: `Given_Feed.When_Value*` for `Feed.Value`, passing
- [x] UI tests: `Given_FeedView` mock-source cases (7 cases; compile-verified — run by runtime-test stages)
- [x] Playground: `FeedViewMockPage` + `JsonDataContext` + `FeedMock` + route + home button + FeedView.xaml merge in App.xaml
- [x] Docs: `doc/Learn/Mvux/FeedView.md` — "Mock data and design-time sources" section
- [x] Release build (net9.0) of `Uno.Extensions.Reactive` and `Uno.Extensions.Reactive.WinUI`: zero warnings
- [x] `dotnet test Uno.Extensions.Reactive.Tests`: full suite green (1443 passed / 19 pre-existing skips)

## Notes / decisions

- The coercion surface is `ISignal<IMessage>` (internal `MockFeed.Create`), not `IFeed<object>` as first
  planned: `IFeed<T>` is invariant, so a typed feed passthrough cannot be typed `IFeed<object>`.
  Public API additions are limited to `Feed.Value<T>(T)`.
- Playground desktop currently only builds with `-p:UnoDisableHotDesign=true`: the Uno.UI.HotDesign
  source generator fails on this machine (pre-existing, reproduced on a clean tree — 100 errors on main).
- `Person Name="..."` in XAML generates an `x:Name` member — set POCO `Name` properties via property
  elements in mock XAML (documented with a comment in `FeedViewMockPage.xaml`).
- Pre-existing on `main` (verified on a clean tree): Playground desktop starts with a blank window —
  initial navigation fails with "Route '' has 19 nested route(s) but none marked IsDefault" even though
  `initialViewModel: typeof(HomeViewModel)` is passed. Worked around in this branch by marking the
  `Home` route `IsDefault: true` in `AppHost.cs`. The underlying resolver behavior (initialViewModel
  not resolving the initial route) is a candidate separate issue.

## Review

Verified live on Playground (net9.0-desktop, 2026-08-22) — all seven demo sections render correctly:

1. POCO value → "Poco Polly / 34" via ValueTemplate
2. POCO envelope Progress=true → ProgressRing + custom ProgressTemplate
3. POCO envelope Error (string) → ErrorTemplate showing the coerced exception message
4. POCO envelope Data=null → NoneTemplate
5. JSON envelope (camelCase keys) → ValueTemplate binding into the ExpandoObject data ("Json Jane / Contoso") —
   confirms Uno's binding engine resolves ExpandoObject members on Skia desktop
6. JSON envelope with "error" string → ErrorTemplate
7. JSON plain object (not an envelope) → whole object as value ("Raw Ray / Not an envelope")

Unit suite: 1443 passed / 0 failed / 19 pre-existing skips.
Release (net9.0) builds of Reactive + Reactive.WinUI: zero warnings.
