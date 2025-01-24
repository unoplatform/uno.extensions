---
uid: Uno.Extensions.Mvux.ListStates
---

# What are list-states

List-state is the collection counterpart of [state](xref:Uno.Extensions.Mvux.States).

List-state adds extra operators which make it easier to apply updates on multiple items instead of just a single item.

Recall that feeds are stateless and are only read-only data output to the View, and are not responding to changes, whereas states are stateful, and update the Model and its entities upon changes made in the View using two-way data-binding, or on demand via Commands.

So an `IState<T>` is a stateful feed of a single item of `T`, whereas an `IListState<T>` is a stateful feed of multiple items of `T`.

## How to create a list-state

The static `ListState` class provides factory methods for creating `IListState<T>` objects, here they are:

### Empty

Creates an empty list-state:

```csharp
IListState<string> MyStrings = ListState<string>.Empty(this);
```

### Value

Creates a list-state with an initial synchronous value:

```csharp
private readonly IImmutableList<string> _favorites =
    new string[]
    {
        "Terry Fox",
        "David Suzuki",
        "Margaret Atwood"
    }
    .ToImmutableArray();

public IListState<string> Favorites => ListState.Value(this, () => _favorites);
```

### Async

Creates a list-state from an async method:

```csharp
public ValueTask<IImmutableList<string>> GetStrings(CancellationToken ct) => new(_favorites);


public IListState<string> Favorites => ListState.Async(this, GetStrings);
```

### AsyncEnumerable

```csharp
public async IAsyncEnumerable<IImmutableList<string>> GetStrings([EnumeratorCancellation] CancellationToken ct)
{
    yield return _favorites;
}

public IListState<string> Favorites => ListState.AsyncEnumerable(this, GetStrings);
```

#### FromFeed

```csharp
public IListFeed<string> FavoritesFeed => ...
public IListState<string> FavoritesState => ListState.FromFeed(this, FavoritesFeed);
```

### Operators

In the following examples, we'll refer to `MyStrings` which is an `IListState<string>`, to demonstrate how to use the various operators `IListState<T>` provides to update its state with modified data.

#### Add

The `AddAsync` method adds an item to the end of the List State:

```csharp
await MyStrings.AddAsync("Gord Downie", cancellationToken);
```

#### Insert

The `InsertAsync` method inserts an item to the beginning of the List State:

```csharp
await MyStrings.InsertAsync("Margaret Atwood", cancellationToken);
```

#### Update

There are various ways to update values in the list-state:

The `Update` method has an `updater` parameter like the `State` does.
This parameter is a `Func<IImmutableList<T>, IImmutableList<T>>`, which when called passes in the existing collection, allows you to apply your modifications to it, and then returns it.

For example:

```csharp
public async ValueTask TrimAll(CancellationToken ct = default)
{
    await MyStrings.Update(
        updater: existing =>
            existing
            .Select(item =>
                item.Trim())
            .ToImmutableList(),
        ct: ct);
}
```

Another overload is `UpdateAllAsync`, which allows you to apply an update on items that match a criteria. The `match` predicate is checked for each item. If the item matches the criteria, the updater is invoked which returns a new instance of the item with the update applied:

```csharp
public async ValueTask TrimLongNames(CancellationToken ct = default)
{
    await MyStrings.UpdateAllAsync(
        match: item => item?.Length > 10,
        updater: item => item.Trim(),
        ct: ct);
}
```

Two more overloads are available for items that implement `IKeyEquatable<T>` :

`UpdateItemAsync` is another overload that allows you to apply an update on items that match the `key` of the specified item. The `key` is validated for each item. For every item that matches, the updater is invoked, returning a new instance of the item with the update applied:

> [!TIP]
> You don't have to implement `IKeyEquatable<T>` by yourself, see [generation](xref:Uno.Extensions.Equality.concept#generation) for more information.

```csharp
public partial record MyItem([property: Key] int Key, string Value);

IListState<MyItem> MyItems = ListState<MyItem>.Value(this, () => new[]
{
    new MyItem(1, "One"),
    new MyItem(2, "Two"),
    new MyItem(3, "Three")
}.ToImmutableList());

public async ValueTask MakeUpperCase(CancellationToken ct = default)
{
    var itemToUpdate = new MyItem(2, "Two");

    await MyItems.UpdateItemAsync(
        oldItem: itemToUpdate,
        updater: item => item with { Value = item.Value.ToUpper()},
        ct: ct);
}
```

Instead of using the `updater`, you can also pass in a new item to replace the old one:

```csharp
public async ValueTask MakeUpperCase(CancellationToken ct = default)
{
    var itemToUpdate = new MyItem(2, "Two");

    await MyItems.UpdateItemAsync(
        oldItem: itemToUpdate,
        newItem: new MyItem(2, "TWO"),
        ct: ct);
}
```

#### Remove

The `RemoveAllAsync` method uses a predicate to determine which items are to be removed:

```csharp
await MyStrings.RemoveAllAsync(
    match: item => item.Contains("Ő"),
    ct: cancellationToken);
```

#### ForEach

This operator can be called from an `IListState<T>` to execute an asynchronous action when the data changes. The action is invoked once for the entire set of data, rather than for individual items:

```csharp
await MyStrings.ForEach(async(list, ct) => await PerformAction(items, ct));

...

private async ValueTask PerformAction(IImmutableList<string> items, CancellationToken ct)
{
    ...
}

```

### Selection operators

Like list-feed, list-state provides out-the-box support for Selection.
This feature enables flagging single or multiple items in the State as 'selected'.

Selection works seamlessly and automatically with the `ListView` and other selection controls.
In case you need to select an item manually for example in response to a button pressed or when finding a searched item, you can use the following methods that enable manual changing of the Selection state of items in the list-state:

#### TrySelectAsync

The `TrySelectAsync` method attempts to find the first occurrence of the item or items passed in as an argument and flag it as 'selected'.

This method comes in two flavors, one that accepts a single item to be selected, while the other one takes multiple.

It returns a boolean value indicating if the desired selection item was found and has been selected.

##### Single item selection

```csharp
IListState<string> Names => ...

private ValueTask SelectCharlie(CancellationToken ct)
{
    bool selected = await Names.TrySelectAsync("charlie", ct);
}
```

##### Multi-item selection

```csharp
IListState<string> Names => ...

private ValueTask SelectCharlieAndJoe(CancellationToken ct)
{
    ImmutableList<string> charlieAndJoe = ImmutableList.Create("charlie", "joe");
    bool selected = await Names.TrySelectAsync(charlieAndJoe, ct);
}
```

#### ClearSelection

The `ClearSelection` method clears the current selection and flags all items as 'not selected':

```csharp
await MyStrings.ClearSelection(cancellationToken);
```

### Subscribing to the selection

You can create a Feed that reflects the currently selected item or items (when using multi-selection) of a Feed.
This is explained in detail in the [Selection page](xref:Uno.Extensions.Mvux.Advanced.Selection).
