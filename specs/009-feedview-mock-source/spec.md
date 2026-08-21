# FeedView mock sources (POCO / JSON envelopes)

## Problem

`FeedView.Source` is declared as `object` but every consumption site casts with `Source as ISignal<IMessage>`
(`FeedView.cs` — `OnSourceChanged`, `Enable`, `OnApplyTemplate`). Any value that is not an `IFeed<T>` /
`IState<T>` is silently discarded: the control stays in its loading state and nothing renders.

This makes it needlessly hard to prototype a page: a designer or developer should be able to hand a
`FeedView` a plain POCO, an anonymous object, or a dictionary produced from a JSON document, and see the
page render — including the *non-Value* states (progress, error, empty) — without writing a view model.

## Goals

1. A non-feed `Source` is wrapped as a single-message feed instead of being discarded.
2. A lightweight "envelope" convention lets the mock drive the `FeedView` state machine
   (Value / None / Error / Indeterminate) — not just the happy path.
3. Both strongly-typed objects (POCO, anonymous types) and loosely-typed objects
   (`IDictionary<string, object?>`, `ExpandoObject`, JSON-derived graphs) are supported.
4. `Refresh` on a mocked source completes instead of hanging `IAsyncCommand.IsExecuting`.
5. No breaking change: any `Source` that is already an `ISignal<IMessage>` behaves exactly as today.

## Non-goals

- Overlaying envelope axes on top of a *real* feed (an envelope whose `Data` is a feed returns that feed
  unchanged; other envelope members are ignored).
- Making arbitrary JSON graphs bindable on every platform. Envelope *detection* is handled by this
  feature; binding *into* the `Data` value from a template relies on the platform binding engine
  (Uno's binding engine supports `ExpandoObject` / `IDictionary<string, object>`; WinAppSDK on Windows
  does not guarantee reflection binding into dynamic objects). The Playground sample converts JSON to
  `ExpandoObject` graphs for this reason.

## Design

### Envelope convention

The envelope mirrors the three values `FeedViewState` exposes to template bindings (`Data`, `Progress`,
`Error`). Property/key matching is **case-insensitive** (JSON is typically camelCase):

| Canonical | Aliases                    | Effect on the mocked feed message                                     |
| --------- | -------------------------- | --------------------------------------------------------------------- |
| `Data`    | —                          | `null` → `Option.None` (None state); otherwise `Option.Some(value)` (Some state). If the value is itself an `ISignal<IMessage>`, that feed is returned directly. |
| `Progress`| `IsProgress`, `InProgress` | `true` → message is transient (`MessageAxis.Progress`) → Indeterminate state; `FeedView` keeps reporting `ILoadable.IsExecuting == true` (by design: it mocks an in-flight load). |
| `Error`   | `Exception`                | non-null → `MessageAxis.Error` → Error state. A `string` value is wrapped in an exception carrying that message (JSON cannot carry an `Exception`). |

**Detection rule** — an object is an envelope iff:

- at least one recognized name is present, **and**
- every public readable instance property (or dictionary key) is within the recognized set.

This keeps real POCOs that happen to expose a `Data` property (e.g. `Report { Data, Title, Author }`)
out of the envelope path: they degrade to "wrap the whole object as the value" instead of being mangled.
An object with no public properties is not an envelope.

Two readers implement the property lookup:

- `IDictionary<string, object?>` (covers `ExpandoObject` and JSON-derived dictionaries) — key lookup is
  `OrdinalIgnoreCase`. This path is trim/AOT-safe.
- Reflection over public instance properties (POCO, anonymous types) — annotated for trimming (see below).

**Value coercion** (loosely-typed path):

- `Progress`: `bool` used as-is; anything else goes through `bool.TryParse(value.ToString(), ...)`,
  defaulting to `false`. This covers JSON `true`, `"true"`, and `JsonElement` without the core package
  referencing `System.Text.Json`.
- `Error` / `Exception`: an `Exception` instance passes through; any other non-null value is wrapped as
  `MockFeedException(value.ToString())` (internal type; travels as `Exception` so consumers are unaffected).

### Coercion pipeline

`object?` → `ISignal<IMessage>?`, applied by `FeedView` when `Source` changes:

1. `null` → no feed (unchanged behavior).
2. Already `ISignal<IMessage>` (any `IFeed<T>` / `IState<T>` / `IListFeed<T>` via `ISignal`'s covariance)
   → passthrough, untouched.
3. Envelope (per rule above) → `MockFeed` with the three axes set. If `Data` is a feed → that feed.
4. Anything else (POCO, string, list, `ExpandoObject` without envelope keys) → `MockFeed` in the
   Some state wrapping the object.

Note: the coerced feed cannot be typed `IFeed<object>` for the passthrough case — `IFeed<T>` is invariant
in `T` (`IFeed<int>` is not `IFeed<object>`), while `ISignal<out T>` covariance makes every `IFeed<T>` an
`ISignal<IMessage>`. The coercion surface is therefore `ISignal<IMessage>`, which is exactly what
`FeedView` consumes.

### New / changed surface

**`Uno.Extensions.Reactive` (core, net9.0 — unit-testable by package CI):**

- `Sources/MockFeed.cs` — `internal sealed class MockFeed : IFeed<object>`. Emits one message carrying
  the mocked `Data` / `Error` / `Progress` axes. Honors `RefreshRequest` by re-emitting the same message
  with the requested `RefreshToken` (`MessageAxis.Refresh`) so `FeedView.Refresh` completes; completes
  its enumeration on `EndRequest` (feeds must be completable — WASM subscriber-leak rule).
  Static `MockFeed.TryCreateEnvelope(object)` / `MockFeed.Create(object)` implement the coercion.
- `Core/Feed.cs` — new public factory `Feed.Value<T>(T value)`: a constant feed over the existing
  internal `ValueFeed<T>` (single message, then completes). Additive; useful beyond mocking (there is
  currently no public factory for a constant feed).

**`Uno.Extensions.Reactive.UI`:**

- `FeedView` caches the coerced source in a private field assigned in `OnSourceChanged`; `Enable` and
  `OnApplyTemplate` read the field instead of re-casting `Source`. Caching preserves feed identity so
  `Subscribe`'s `_subscription?.Feed == feed` short-circuit keeps working (no re-subscription on every
  load/template pass). The public `Source` property is unchanged and still returns the raw object.

### Refresh semantics on a mock

`RefreshCommand.Execute` clears `IsExecuting` only when a message arrives carrying the requested
`RefreshToken` and the message is non-transient. `MockFeed` therefore:

- registers the token on the request and re-emits its message with `MessageAxis.Refresh` set → refresh
  completes for Value / None / Error mocks;
- for a `Progress = true` mock the re-emitted message is still transient, so `IsExecuting` remains true —
  consistent with refreshing a feed whose load never finishes (that is what the mock models).

### Trimming / AOT

The reflection reader is annotated `[RequiresUnreferencedCode]`; the single call site inside the coercion
carries a scoped `#pragma warning disable IL2026` with justification (same precedent as
`AsyncFeed.BuildDependentTypeSet`). Under aggressive trimming, envelope detection on a POCO can fail
soft: the object falls back to being wrapped whole as a value — never a crash. The dictionary /
`ExpandoObject` path is reflection-free and is the documented trim-safe option.

## Spec deviations (Exceptions process)

- **JSON→object conversion in the Playground sample** traverses `JsonElement` rather than using a typed,
  source-generated deserializer (repo rule §2/§12). Constraint: the entire point of the JSON mock path is
  an *unknown-at-compile-time* shape, so no typed model can exist. Impact: sample-only code
  (`samples/Playground`), never shipped in a package. Mitigation: conversion is isolated in one small
  helper (`JsonDataContext`) and the library itself never parses JSON.

## Test plan

`src/Uno.Extensions.Reactive.Tests/Sources/Given_MockFeed.cs` (package CI):

- passthrough: a real feed / an envelope whose `Data` is a feed → same instance returned;
- raw POCO / string / list → one Some message, then completes on `EndRequest`;
- envelope: `Data` set → Some; `Data = null` → None; `Progress = true` → transient;
  `Error` non-null → error axis; combinations (error + data, progress + data);
- false-positive guard: POCO with `Data` plus unrecognized properties → treated as raw value;
- dictionary/`ExpandoObject` path: camelCase keys, string `error` value, string `"true"` progress;
- refresh: `RequestRefresh` → second message carrying the refresh token.

`src/Uno.Extensions.Reactive.UI.Tests/Given_FeedView.cs` (runtime-test stages):

- POCO `Source` → `ValueTemplate` renders / `FeedViewState.Data` exposes the POCO;
- envelope `Progress = true` → `ILoadable.IsExecuting` stays true;
- envelope `Error` → `FeedViewState.Error` set; `Data = null` → None state;
- re-applying template / reloading with same `Source` object → no re-subscription;
- `Refresh` on a mock completes (`IsExecuting` returns to false).

## Documentation

- `doc/Learn/Mvux/FeedView.md`: new "Mock data and design-time sources" section — envelope table,
  detection rule, POCO + JSON examples, trim-safe note, `Feed.Value` mention.

## Playground demo

`samples/Playground` gets a `FeedViewMockPage` (route `FeedViewMock`, button on `HomePage`) showing:

- a `FeedView` whose `Source` is a POCO (`Person`) declared in XAML;
- a `FeedView` bound to envelope objects declared in XAML (data / progress / error variants);
- a `FeedView` whose `DataContext` is set in XAML from a raw JSON string via a small `JsonDataContext`
  helper (JSON → `ExpandoObject` graph), with `Source="{Binding Value}"`.
