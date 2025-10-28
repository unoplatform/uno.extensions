---
uid: Uno.Extensions.Mvux.ListStates
---

# MVUX List-State Guide

Compact reference for creating and manipulating `IListState<T>` within MVUX models.

## TL;DR
- `IListState<T>` is the mutable (via snapshots) list counterpart to `IState<T>`; use it for collections that accept updates from the view.
- Compose list-states with `ListState.Empty`, `ListState.Value`, `ListState.Async`, `ListState.AsyncEnumerable`, or `ListState.FromFeed`.
- Apply updates with built-in helpers such as `AddAsync`, `InsertAsync`, `Update*`, `RemoveAllAsync`, `ForEach`, and selection APIs (`TrySelectAsync`, `ClearSelection`).

## Why Use List-State
- Keeps MVUX data flow unidirectional while handling collection updates triggered by two-way bindings or commands.
- Adds list-specific operators so you don’t have to clone immutable lists manually.
- Works with selection-enabled XAML controls out of the box.

## Creating an `IListState<T>`
```csharp
// Empty list
IListState<string> Names = ListState<string>.Empty(this);

// Pre-populated synchronous value
public IListState<string> Favorites =>
    ListState.Value(this, () => ImmutableArray.Create("Terry Fox", "David Suzuki", "Margaret Atwood"));

// Async factory
public IListState<string> Recent =>
    ListState.Async(this, ct => FetchRecentAsync(ct));

// Streaming updates
public IListState<string> Streamed =>
    ListState.AsyncEnumerable(this, GetSnapshots);

// Bridge an IListFeed
public IListState<string> FromFeed =>
    ListState.FromFeed(this, FavoritesFeed);
```

## Mutating the Collection
```csharp
await Names.AddAsync("Gord Downie", ct);          // append
await Names.InsertAsync("Margaret Atwood", ct);   // insert at start

await Names.Update(existing =>
    existing.Select(item => item.Trim()).ToImmutableList(), ct);

await Names.UpdateAllAsync(item => item.Length > 10,
    item => item.Trim(), ct);
```

### Keyed Updates
```csharp
public partial record MyItem([property: Key] int Id, string Value);

await Items.UpdateItemAsync(
    oldItem: new MyItem(2, "Two"),
    updater: item => item with { Value = item.Value.ToUpperInvariant() },
    ct: ct);

await Items.UpdateItemAsync(
    oldItem: new MyItem(3, "Three"),
    newItem: new MyItem(3, "THREE"),
    ct: ct);
```

### Removal & Iteration
```csharp
await Names.RemoveAllAsync(item => item.Contains("Ő"), ct);

await Names.ForEach(async (items, token) =>
{
    await LogAsync(items, token);
});
```

## Selection Helpers
- `TrySelectAsync(item)` / `TrySelectAsync(items)` — flag one or many entries as selected; returns `bool` indicating success.
- `ClearSelection(ct)` — clear current selection.
- Subscribe to selection changes with feeds as described in (xref:Uno.Extensions.Mvux.Advanced.Selection).

```csharp
bool selected = await Names.TrySelectAsync("charlie", ct);
await Names.ClearSelection(ct);
```

## See Also
- [State basics](xref:Uno.Extensions.Mvux.States)
- [Selection](xref:Uno.Extensions.Mvux.Advanced.Selection)
- [Equality generation](xref:Uno.Extensions.Equality.concept#generation)
