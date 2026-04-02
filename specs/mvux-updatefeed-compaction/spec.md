# UpdateFeed Compaction

**Status:** Draft
**Affects:** `Uno.Extensions.Reactive` — `UpdateFeed<T>`, `IFeedUpdate<T>`, `StateImpl<T>`, `ListFeedSelection`

## Problem

`UpdateFeed<T>.UpdateFeedSource` accumulates every `IFeedUpdate<T>` in `_activeUpdates` (`ImmutableList<IFeedUpdate<T>>`). The only pruning path is `RebuildMessage()`, which calls `IsActive()` on each update and removes inactive ones. However `RebuildMessage()` is only triggered by:

1. A parent source message (`OnParentUpdated`)
2. A `Remove` operation in `OnUpdateReceived`

When the parent source completes — the common case with `ListState.Value(this, () => initialValue)` which produces one message then terminates — no further parent updates arrive. All subsequent mutations flow through `IncrementalUpdate()`, which **never calls `IsActive()`** and **never prunes `_activeUpdates`**.

Each `AddAsync` / `UpdateAsync` call creates a `StateImpl.Update` record retained forever. 500 mutations = 500 retained `Update` records (~600 KB residual after GC on desktop, worse on WASM where `memory.grow` is irreversible).

### Why existing `IsActive` cannot solve this

`StateImpl.Update.IsActive` for Volatile updates (`StateImpl.cs:171-173`):

```csharp
=> !_firstResult.Task.IsFaulted
    && (Kind is StateUpdateKind.Persistent || !parentChanged || !_firstResult.Task.IsCompleted);
```

When the parent source has completed, `parentChanged` is never `true` again. So `!parentChanged` is always `true`, the Volatile deactivation condition never fires, and the update stays in `_activeUpdates` indefinitely.

### Why `ToImmutableList()` materialization doesn't help

`ConversationPanelModel.PostMessageAsync` attempts to mitigate this by calling `Messages.UpdateAsync(messages => messages.ToImmutableList(), ct)` after every add (when count > 50). This breaks the `ImmutableList` structural sharing chain but **doubles the number of MVUX pipeline updates** (AddAsync + UpdateAsync per message). The `Update<>` entries still accumulate — the materialization makes it worse.

## Design

### `IFeedUpdate<T>.IsCompactable`

Add a method following the `IsActive()` pattern:

```csharp
internal interface IFeedUpdate<T>
{
    bool IsActive(Message<T>? parent, bool parentChanged, IMessageEntry<T> message);
    void Apply(bool parentChanged, MessageBuilder<T, T> message);

    /// <summary>
    /// Determines whether this update can be removed from the active updates list
    /// after it has been applied. Called only after Apply has succeeded at least once.
    /// When an update is compacted, its effect remains baked into the current message
    /// but the update record is released for GC.
    /// </summary>
    /// <param name="parentCompleted">
    /// True when the parent source feed has completed (no more parent messages will arrive).
    /// </param>
    /// <returns>
    /// True if this update does not need to be retained for replay or rollback.
    /// </returns>
    bool IsCompactable(bool parentCompleted) => false;
}
```

### `IFeedRollbackableUpdate<T>`

For updates that are compactable but may later need to be `Remove`'d:

```csharp
internal interface IFeedRollbackableUpdate<T>
{
    /// <summary>
    /// Undoes this update's effect on the current message. Called when a Remove
    /// targets an update that was already compacted from the active list.
    /// </summary>
    void Rollback(MessageManager<T, T> message);
}
```

### Per-implementation behavior

#### `StateImpl.Update` (most common — the leak source)

```csharp
private record Update(Action<MessageBuilder<T>> Method, StateUpdateKind Kind) : IFeedUpdate<T>
{
    // ... existing ...

    public bool IsCompactable(bool parentCompleted)
        => parentCompleted && _firstResult.Task.IsCompletedSuccessfully;
}
```

- **No `IFeedRollbackableUpdate` needed.** `StateImpl.UpdateMessageAsync` only calls `_inner.Add(update)` — there is no code path that calls `_inner.Remove(update)` or `_inner.Replace(old, update)` for these records.
- Both Volatile and Persistent updates are compactable once parent completes — the parent will never change again, so there is nothing to re-apply against.
- Faulted updates already handled by `IsActive` returning `false`.

#### `SelectionFeedUpdate`

```csharp
private class SelectionFeedUpdate : IFeedUpdate<IImmutableList<TSource>>
{
    // ... existing ...

    public bool IsCompactable(bool parentCompleted) => true;
}
```

- **Always compactable.** Each `SelectionFeedUpdate` overwrites the `Selection` axis entirely — the effect is additive, not incremental.
- **No `IFeedRollbackableUpdate` needed.** `Replace(old, new)` is used for cleanliness, but the new update's `Apply` fully overwrites the selection. If `old` was compacted, the Remove side of the Replace is a no-op (old isn't in `_activeUpdates`), and `new` is added and applied — correct behavior since `new` sets the selection from scratch.

#### `FeedUpdate<T>` (generic delegate-based)

```csharp
internal record FeedUpdate<T>(
    Func<Message<T>?, bool, IMessageEntry<T>, bool> IsActive,
    Action<bool, MessageBuilder<T, T>> Apply,
    Action<MessageManager<T, T>>? Rollback = null) : IFeedUpdate<T>, IFeedRollbackableUpdate<T>
{
    bool IFeedUpdate<T>.IsActive(...) => IsActive(...);
    void IFeedUpdate<T>.Apply(...) => Apply(...);

    bool IFeedUpdate<T>.IsCompactable(bool parentCompleted)
        => parentCompleted && Rollback is not null;

    void IFeedRollbackableUpdate<T>.Rollback(MessageManager<T, T> message)
        => Rollback?.Invoke(message);
}
```

- Compactable only when a `Rollback` delegate is explicitly provided — opt-in safety.
- The `Rollback` constructor parameter is optional with default `null` — existing callers unaffected.

## `UpdateFeedSource` changes

### Track parent source completion

```csharp
private class UpdateFeedSource : IAsyncEnumerable<Message<T>>
{
    // ... existing fields ...
    private bool _parentCompleted;                                                     // NEW
    private ImmutableDictionary<IFeedUpdate<T>, IFeedRollbackableUpdate<T>> _compactedUpdates // NEW
        = ImmutableDictionary<IFeedUpdate<T>, IFeedRollbackableUpdate<T>>.Empty;

    public UpdateFeedSource(UpdateFeed<T> owner, SourceContext context, CancellationToken ct)
    {
        // ... existing init ...

        owner._updates.ForEachAsync(OnUpdateReceived, ct);

        // Capture the parent source Task to detect completion
        context.GetOrCreateSource(owner._source)
            .ForEachAsync(OnParentUpdated, ct)
            .ContinueWith(
                _ =>
                {
                    lock (this)
                    {
                        _parentCompleted = true;
                        TryCompact();
                    }
                },
                TaskContinuationOptions.ExecuteSynchronously);
    }
```

### Compaction after incremental update

```csharp
private void OnUpdateReceived((IFeedUpdate<T>[]? added, IFeedUpdate<T>[]? removed) args)
{
    lock (this)
    {
        if (!_isParentReady)
            return;

        bool needsUpdate = false, canDoIncrementalUpdate = !_isInError;

        if (args.removed is { Length: > 0 } removed)
        {
            var updates = _activeUpdates.RemoveRange(removed);
            if (_activeUpdates != updates)
            {
                needsUpdate = true;
                canDoIncrementalUpdate = false;
                _activeUpdates = updates;
            }

            // Handle Remove of previously compacted updates
            foreach (var r in removed)
            {
                if (_compactedUpdates.TryGetValue(r, out var rollbackable))
                {
                    _compactedUpdates = _compactedUpdates.Remove(r);
                    needsUpdate = true;
                    canDoIncrementalUpdate = false;
                    // Rollback applies the undo on the current message
                    rollbackable.Rollback(_message);
                }
            }
        }

        if (args.added is { Length: > 0 } added)
        {
            var updates = _activeUpdates.AddRange(added);
            if (_activeUpdates != updates)
            {
                needsUpdate = true;
                _activeUpdates = updates;

                if (canDoIncrementalUpdate)
                {
                    IncrementalUpdate(added);
                    TryCompact();           // <-- NEW
                    return;
                }
            }
        }

        if (needsUpdate)
        {
            RebuildMessage(parentMsg: default);
        }
    }
}
```

### `TryCompact()`

```csharp
/// <summary>
/// Removes compactable updates from _activeUpdates, keeping their
/// effect baked into the current message. Must be called under lock(this).
/// </summary>
private void TryCompact()
{
    if (!_parentCompleted || _activeUpdates.IsEmpty)
        return;

    var compacted = _activeUpdates;
    foreach (var update in _activeUpdates)
    {
        if (update.IsCompactable(_parentCompleted))
        {
            compacted = compacted.Remove(update);

            // Track rollbackable updates in case of later Remove
            if (update is IFeedRollbackableUpdate<T> rollbackable)
            {
                _compactedUpdates = _compactedUpdates.Add(update, rollbackable);
            }
        }
    }

    _activeUpdates = compacted;
}
```

### Replace of compacted update

`Replace(old, new)` signals `(added: [new], removed: [old])`. When `old` was already compacted:

1. The `removed` processing checks `_compactedUpdates` — if `old` has a `Rollback`, it is invoked, removed from `_compactedUpdates`, and `canDoIncrementalUpdate` is set to `false`
2. The `added` processing adds `new` to `_activeUpdates`
3. Falls through to `RebuildMessage` (since `canDoIncrementalUpdate` is false)
4. `RebuildMessage` replays all active updates (including `new`) from the current parent

For `SelectionFeedUpdate` (no `IFeedRollbackableUpdate`): `old` isn't in `_activeUpdates` and isn't in `_compactedUpdates`, so the Remove side is a no-op. `new` is added and applied via `IncrementalUpdate`. Correct — `new` fully overwrites the selection axis.

## Thread safety

All paths that read/mutate `_activeUpdates`, `_compactedUpdates`, and `_parentCompleted` run under `lock(this)`:

| Path | Lock | Fields accessed |
|---|---|---|
| `OnUpdateReceived` | `lock(this)` | all |
| `OnParentUpdated` | `lock(this)` | `_activeUpdates` |
| `ContinueWith` callback | `lock(this)` | `_parentCompleted`, `_activeUpdates`, `_compactedUpdates` |
| `TryCompact` (private) | caller holds lock | `_parentCompleted`, `_activeUpdates`, `_compactedUpdates` |

Lock ordering: `lock(this)` then `MessageManager._gate` (inside `_message.Update`). Same as existing `IncrementalUpdate` and `RebuildMessage`. No deadlock risk.

## Edge cases

- **Parent source never completes:** `_parentCompleted` stays `false`. `IsCompactable(false)` returns `false` for `StateImpl.Update`. `SelectionFeedUpdate` returns `true` — safe since selection is always overwritten. No behavior change for the common case.
- **Error state:** `canDoIncrementalUpdate` is `false` when `_isInError`, so `RebuildMessage` runs (prunes via `IsActive`). `TryCompact` is only called after `IncrementalUpdate`, so not reached during errors.
- **`_waitForParent` active:** Updates ignored while `!_isParentReady`. No compaction.
- **`_compactedUpdates` unbounded growth:** Only grows for `IFeedRollbackableUpdate` updates (`FeedUpdate<T>` with explicit `Rollback`). `StateImpl.Update` is not rollbackable. `SelectionFeedUpdate` is not rollbackable. Bounded by design.

## Files to modify

| File | Change |
|---|---|
| `src/Uno.Extensions.Reactive/Operators/UpdateFeed.cs` | Add `IsCompactable` to `IFeedUpdate<T>`. Add `IFeedRollbackableUpdate<T>`. Extend `FeedUpdate<T>` with optional `Rollback`. Add `_parentCompleted`, `_compactedUpdates`, `TryCompact()` to `UpdateFeedSource`. Modify `OnUpdateReceived`. Chain `ContinueWith` on parent `ForEachAsync`. |
| `src/Uno.Extensions.Reactive/Core/Internal/StateImpl.cs` | Add `IsCompactable` to `Update` record. |
| `src/Uno.Extensions.Reactive/Operators/ListFeedSelection.cs` | Add `IsCompactable` to `SelectionFeedUpdate`. |

## Test plan

1. **Compaction after parent completes:** `ListState.Value(initialValue)`, 500 `AddAsync` calls, verify `_activeUpdates` stays bounded (memory test).
2. **No compaction while parent active:** Ongoing source feed, verify `_activeUpdates` retains entries until parent rebuild.
3. **Remove of compacted update with rollback:** `FeedUpdate<T>` with `Rollback` delegate, compact, then `Remove`, verify rollback invoked and message rebuilt.
4. **Replace of compacted update:** `SelectionFeedUpdate` compacted, then `Replace(old, new)`, verify `new` applied correctly.
5. **Persistent updates compacted after parent completes:** `StateUpdateKind.Persistent`, verify compacted once parent source terminates.
6. **Error recovery:** Error in update, verify compaction does not interfere with `_isInError` rebuild path.

## Migration / Breaking changes

- `IFeedUpdate<T>` gains `IsCompactable` with default interface implementation returning `false`. Internal interface — no external break. All 3 implementors updated in this change.
- `FeedUpdate<T>` record gains optional `Rollback` parameter with default `null`. Existing callers unaffected.
- No behavioral change for feeds with active parent sources. Compaction only activates after parent completion.
