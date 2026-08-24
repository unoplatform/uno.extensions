# Spec 009: Hot Testing Reactive feed mocks

Status: Implemented for issue #3149

## Product direction

This is the deliberately small runtime foundation requested for
[uno.extensions #3149](https://github.com/unoplatform/uno.extensions/issues/3149).

- The assembly, package, and namespace are exactly Uno.HotTesting.Reactive, even
  though the project lives in the uno.extensions repository for now.
- The public vocabulary is FeedMock and ListFeedMock.
- There is no generated code.
- Users hand-write a view-model-shaped record or class, make its property names and
  feed shapes match their XAML bindings, and assign that object to Page.DataContext.
- Name matching, binding shape correctness, and DataContext injection are the user's
  responsibility.
- This runtime layer must remain reusable by the fuller spec 013 generation and
  architecture work later.

The name ListFeedMock is authoritative. An earlier conversation used ListViewMock once,
but the issue and the proven runtime work in PR #3147 both refer to a list feed rather
than a view.

## Public API

FeedMock and ListFeedMock expose the same small state vocabulary:

- Undefined<T>()
- Loading<T>()
- Empty<T>()
- Value<T>(...)
- Error<T>(Exception)
- Refreshing<T>(...)
- Message<T>(...)

Message is retained as the low-level escape hatch because MVUX messages have
independent data, error, progress, and other axes. It keeps this runtime usable by
future generated factories without adding state scripting or generator policy now.

There is intentionally no public state enum in this first slice. The factories are
the reusable primitive; an enum would add a second abstraction before there is a
consumer that needs it.

## Semantics

Every factory returns a cold, finite feed. Each subscription receives its pinned
message state and completes. Loading and refreshing therefore remain visually pinned
without a never-ending task, timer, or background operation. A later subscription
(such as a view being unloaded and reparented) receives the same state again.

Undefined<T>() emits a None data message followed by Undefined. MVUX's initial message
is already undefined, so this transition is required for the final undefined value to
remain an observable data-axis change through state replay.

Refreshing sets data plus transient progress. It does not set the internal refresh
axis, which cannot be produced outside a refreshable source feed.

Scalar FeedMock.Empty<T>() means Option<T>.None.

ListFeedMock.Empty<T>() means a present empty immutable list (Some(empty)). The list
adapter forwards the message rather than using the normal list-feed adapter, which
would coerce Some(empty) to None. Empty Value and Refreshing inputs preserve the same
Some(empty) behavior. Callers that need None for a list can express it explicitly
through ListFeedMock.Message.

## Reuse boundary

Future generator or Hot Design work may consume these factories, but must not fork
their message construction semantics. The runtime has no dependency on a generated
view model, UI framework, reflection, naming convention, or source generator.

## Explicitly rejected scope

The following are not part of issue #3149:

- source generators or generated view-model factories;
- generated user records or automatic property-name matching;
- MockCommand;
- timed scripts or selection helpers;
- gallery, marketing, or broad documentation work.

These exclusions keep this commit a reusable runtime foundation rather than a partial
implementation of the full spec 013 system.

## Verification

Focused tests cover scalar and list common states, all configured message axes, finite
completion, repeated subscriptions, undefined replay through state, Some(empty) list
behavior, assembly/namespace naming, and the public API shape.

Validation on the issue branch:

- Release test build and execution: 22 passed, 0 failed. The uno-dev image has only
  the .NET 10 runtime, so the net9.0 test host was run with DOTNET_ROLL_FORWARD=Major.
- Release package creation succeeded for Uno.HotTesting.Reactive, including its net9.0
  assembly, XML documentation, symbols, and Uno.Extensions.Reactive dependency.
