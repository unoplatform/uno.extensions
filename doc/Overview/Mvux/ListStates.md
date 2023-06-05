---
uid: Overview.Mvux.ListStates
---

# What are List-States

List-State is the collection counterpart of [State](xref:Overview.Mvux.States).
It's a State that adds extra operators which make it easier to apply updates on multiple items instead of just a single one, like what a State does.

Let's recall that Feeds are stateless and are only read-only data output to the View, and is not responding to changes, whereas States are stateful, and update the Model and its entities upon changes made in the View using two-way data-binding, or on demand via Commands.

So an `IState<T>` is a stateful feed of a single item of `T`, whereas an `IListState<T>` is a stateful feed of multiple items of `T`.

# How to create a List-State

The static `ListState` class provides factory methods for creating `IListState<T>` objects, here they are:

## Empty

Creates an empty List-State:

```csharp
IListState<string> MyStrings = ListState<string>.Empty(this);
```

## Value

Creates a List-State with an initial synchronous value:

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

## Async

Creates a List-State from an async method:

```csharp
public ValueTask<IImmutableList<string>> GetStrings(CancellationToken ct) => new(_favorites);


public IListState<string> Favorites => ListState.Async(this, GetStrings);
```

## AsyncEnumerable

```csharp
public async IAsyncEnumerable<IImmutableList<string>> GetStrings([EnumeratorCancellation] CancellationToken ct)
{
    yield return _favorites;
}

public IListState<string> Favorites => ListState.AsyncEnumerable(this, GetStrings);
```

## FromFeed

```csharp
public IListFeed<string> FavoritesFeed => ...
public IListState<string> FavoritesState => ListState.FromFeed(this, FavoritesFeed);
```

## Others

There's also the `ListState.Create` method which allows you to manually build the List-State with Messages.  
You can learn about manual creation of Messages [here](xref:Overview.Reactive.State#create).

# How to change the state of a List-State

In the following examples, we'll refer to `MyStrings` which is an `IListState<string>`, to demonstrate how to use the various operators the List-State provides to update its state with modified data.

## Add

The `AddAsync` method adds an item to the end of the List State:

```csharp
await MyStrings.AddAsync("Gord Downie", cancellationToken);
```

## Insert

The `InsertAsync` method inserts an item to the beginning of the List State:

```csharp
await MyStrings.InsertAsync("Margaret Atwood", cancellationToken);
```

## Update

There are various way to update values in the List-State:

The `Update` method has an `updater` parameter like the `State` does.  
This parameter is a `Func<IImmutableList<T>, IImmutableList<T>>`, which when called passes in the existing collection, allows you to apply your modifications to it then returns it.

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

Another overload is `UpdateAsync`, which allows you to to apply an update on a select item criteria, using a predicate which is checked before updating an individual update, and if item qualifies, uses the `updater` argument, which in this case is a `Func<T, T>` which applies to an individual item:

```csharp
public async ValueTask TrimLongNames(CancellationToken ct = default)
{
    await MyStrings.UpdateAsync(
        match: item => item?.Length > 10,
        updater: item => item.Trim(),
        ct: ct);
}
```

> [!Note]  
> There is also `UpdateData` which allows for manual creation of a data-axis Message wrapped in an `Option<T>` that denotes whether the data has entities or is empty.

## Remove

The `RemoveAllAsync` method uses a predicate to determine which items are to be removed:

```csharp
    await MyStrings.RemoveAllAsync(
        match: item => item.Contains("Ő"),
        ct: cancellationToken);
```

## Selection

List-State provides out-the-box support for Selection.  
This feature enables flagging a single or multiple items in the State as 'selected'.
It even works seamlessly and automatically with the `ListView` and other collection controls.

The following couple of methods enable changing the Selection state of items in the List-State:

## TrySelectAsync

The `TrySelectAsync` method attempts to find the first occurrence of the item passed in as argument and flag it as 'selected':

```csharp
await MyStrings.TrySelectAsync(cancellationToken);
```

## ClearSelection

The `ClearSelection` method clears the current selection and flags all items as 'not selected':

```csharp
await MyStrings.ClearSelection(cancellationToken);
```
