# SingleValueFeed

**Status:** Draft
**Affects:** `Uno.Extensions.Reactive` — `StateImpl<T>`, `AsyncFeed<T>`, `UpdateFeed<T>` compaction

## Problem

`State.Value(this, () => initialValue)` and `ListState.Value(this, () => initialValue)` capture a fixed value at creation time. This value is then wrapped in an `AsyncFeed<T>` via:

```csharp
public StateImpl(SourceContext context, Option<T> defaultValue)
    : this(context, new AsyncFeed<T>(async _ => defaultValue), SubscriptionMode.Eager)
```

`AsyncFeed<T>` is designed to support refresh — it subscribes to `RefreshRequest` and `EndRequest` from the `SourceContext`, and only completes its internal `loadRequests` subject when **both** the local refresh signal has ended **and** an `EndRequest` has been received. For a fixed value that will never change, this is useless — the refresh machinery keeps the feed alive indefinitely.

This prevents the `UpdateFeed<T>` compaction mechanism from activating, since compaction requires `_isParentCompleted = true`, which depends on the parent feed's `ForEachAsync` completing.

### All affected code paths

All `State.Value` / `ListState.Value` / `State<T>.Empty` / `ListState<T>.Empty` overloads end up at:

- `SourceContext.CreateState(Option<T>)` → `new StateImpl<T>(ctx, initialValue)` → `new AsyncFeed<T>(async _ => defaultValue)`
- `SourceContext.CreateListState(Option<IImmutableList<T>>)` → same chain

## Design

### `ValueFeed<T>`

A minimal `IFeed<T>` that produces a single `Message<T>` with the given value and immediately completes.

```csharp
internal sealed class ValueFeed<T> : IFeed<T>
{
    private readonly Option<T> _value;

    public ValueFeed(Option<T> value)
    {
        _value = value;
    }

    public async IAsyncEnumerable<Message<T>> GetSource(
        SourceContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return Message<T>.Initial.With().Data(_value);
    }
}
```

Key properties:

- **Produces exactly one message** then the `IAsyncEnumerable` completes.
- **No refresh support** — does not subscribe to `RefreshRequest` or `EndRequest`.
- **`ForEachAsync` completes immediately** after the first message, which sets `_isParentCompleted = true` in `UpdateFeedSource`, enabling compaction.

### `StateImpl` change

Replace the `AsyncFeed<T>` wrapper with `ValueFeed<T>` for fixed-value states:

```csharp
public StateImpl(SourceContext context, Option<T> defaultValue)
    : this(context, new ValueFeed<T>(defaultValue), SubscriptionMode.Eager)
{
}
```

## Impact

- `UpdateFeed<T>` compaction activates immediately after the `ValueFeed` completes (one message), enabling GC of accumulated `StateImpl.Update` records.
- No behavior change for states backed by real feeds (`Feed.Async`, paginated feeds, etc.) — those still use `AsyncFeed<T>`.
- Refresh requests on a `ValueFeed`-backed state are silently ignored (the feed has already completed). This is correct — refreshing a fixed value is a no-op.

## Files to modify

| File | Change |
|---|---|
| `src/Uno.Extensions.Reactive/Sources/ValueFeed.cs` | New file — `ValueFeed<T>` implementation. |
| `src/Uno.Extensions.Reactive/Core/Internal/StateImpl.cs` | Replace `new AsyncFeed<T>(async _ => defaultValue)` with `new ValueFeed<T>(defaultValue)` in the fixed-value constructor. |

## Test plan

1. **ValueFeed produces one message and completes:** Subscribe, verify exactly one message, verify `ForEachAsync` task completes.
2. **State.Value compaction works end-to-end:** 100 `UpdateAsync` calls on a `State.Value`, verify `_activeUpdates` is compacted to 0.
3. **Data preserved after compaction:** Updates after compaction still work correctly.
4. **Existing State/ListState behavior unchanged:** Full test suite passes.
