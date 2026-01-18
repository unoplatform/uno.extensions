---
name: mvux-listfeed
description: Create and use IListFeed<T> for reactive collections in MVUX. Use when loading lists from services, displaying collections, filtering data, or implementing paginated lists.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# MVUX ListFeed

`IListFeed<T>` represents a reactive, read-only collection that supports automatic loading states, filtering, and pagination.

## Prerequisites

- Add `MVUX` to `<UnoFeatures>` in your `.csproj`

```xml
<UnoFeatures>MVUX</UnoFeatures>
```

## What is a ListFeed?

An `IListFeed<T>` represents:
- A reactive collection from an async source
- Read-only list data with loading/error states
- Support for filtering and pagination
- Automatic refresh capability

## Creating ListFeeds

### From Async Method

```csharp
using Uno.Extensions.Reactive;
using System.Collections.Immutable;

public partial record PeopleModel(IPeopleService Service)
{
    public IListFeed<Person> People => 
        ListFeed.Async(Service.GetPeopleAsync);
}
```

### Service Implementation

```csharp
public interface IPeopleService
{
    ValueTask<IImmutableList<Person>> GetPeopleAsync(CancellationToken ct);
}

public class PeopleService : IPeopleService
{
    public async ValueTask<IImmutableList<Person>> GetPeopleAsync(CancellationToken ct)
    {
        var people = await _repository.GetAllAsync(ct);
        return people.ToImmutableList();
    }
}
```

### From AsyncEnumerable (Push-based)

```csharp
public partial record StockModel(IStockService Service)
{
    // List that updates in real-time
    public IListFeed<Stock> Stocks => 
        ListFeed.AsyncEnumerable(Service.GetStockUpdates);
}
```

Service implementation:

```csharp
public async IAsyncEnumerable<IImmutableList<Stock>> GetStockUpdates(
    [EnumeratorCancellation] CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        var stocks = await _repository.GetAllAsync(ct);
        yield return stocks.ToImmutableList();
        
        await Task.Delay(TimeSpan.FromSeconds(5), ct);
    }
}
```

### With Refresh Signal

```csharp
public partial record PeopleModel(IPeopleService Service)
{
    public Signal RefreshSignal { get; } = new();
    
    public IListFeed<Person> People => 
        ListFeed.Async(Service.GetPeopleAsync, refresh: RefreshSignal);
}
```

## Displaying ListFeed in XAML

### Direct Binding to ListView

```xml
<ListView ItemsSource="{Binding People}">
    <ListView.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBlock Text="{Binding FirstName}" />
                <TextBlock Text="{Binding LastName}" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

### With FeedView (Recommended)

```xml
<mvux:FeedView Source="{Binding People}">
    <DataTemplate>
        <ListView ItemsSource="{Binding Data}">
            <ListView.Header>
                <Button Content="Refresh" Command="{Binding Refresh}" />
            </ListView.Header>
            <ListView.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding FirstName}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </DataTemplate>
    
    <mvux:FeedView.ProgressTemplate>
        <DataTemplate>
            <ProgressRing IsActive="True" />
        </DataTemplate>
    </mvux:FeedView.ProgressTemplate>
    
    <mvux:FeedView.NoneTemplate>
        <DataTemplate>
            <TextBlock Text="No people found" />
        </DataTemplate>
    </mvux:FeedView.NoneTemplate>
</mvux:FeedView>
```

## Filtering ListFeed

### With Where Operator

```csharp
public partial record PeopleModel(IPeopleService Service)
{
    public IListFeed<Person> AllPeople => 
        ListFeed.Async(Service.GetPeopleAsync);
    
    // Filter the list
    public IListFeed<Person> Adults => 
        AllPeople.Where(person => person.Age >= 18);
}
```

### With State-based Criteria

```csharp
public partial record SearchCriteria(string? Term, bool ActiveOnly);

public partial record PeopleModel(IPeopleService Service)
{
    // Filter criteria state
    public IState<SearchCriteria> Criteria => 
        State.Value(this, () => new SearchCriteria(null, false));
    
    // ListFeed that reloads when criteria changes
    public IListFeed<Person> People => 
        Criteria
            .SelectAsync(async (c, ct) => 
                await Service.SearchPeopleAsync(c.Term, c.ActiveOnly, ct))
            .AsListFeed();
}
```

XAML for filter controls:

```xml
<StackPanel Spacing="8">
    <TextBox Header="Search" 
             Text="{Binding Criteria.Term, Mode=TwoWay}" />
    <ToggleSwitch Header="Active Only" 
                  IsOn="{Binding Criteria.ActiveOnly, Mode=TwoWay}" />
    
    <mvux:FeedView Source="{Binding People}">
        <DataTemplate>
            <ListView ItemsSource="{Binding Data}">
                <!-- Item template -->
            </ListView>
        </DataTemplate>
    </mvux:FeedView>
</StackPanel>
```

## Paginated ListFeed

### Index-based Pagination

```csharp
public partial record PeopleModel(IPeopleService Service)
{
    public IListFeed<Person> People => 
        ListFeed.PaginatedAsync<Person>(async (request, ct) => 
            await Service.GetPeoplePageAsync(
                request.DesiredSize ?? 20, 
                request.CurrentCount, 
                ct));
}
```

Service implementation:

```csharp
public async ValueTask<IImmutableList<Person>> GetPeoplePageAsync(
    uint pageSize, 
    uint startIndex, 
    CancellationToken ct)
{
    var people = await _repository.GetPageAsync((int)startIndex, (int)pageSize, ct);
    return people.ToImmutableList();
}
```

### Cursor-based Pagination

```csharp
public partial record VideoModel(IVideoService Service)
{
    public IState<string> SearchQuery => State<string>.Empty(this);
    
    public IListFeed<Video> Videos => 
        SearchQuery
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .SelectPaginatedByCursorAsync(
                firstPage: string.Empty,
                getPage: async (query, cursor, count, ct) =>
                {
                    var page = await Service.SearchAsync(query, cursor, count ?? 20, ct);
                    return new PageResult<string, Video>(
                        page.Videos, 
                        page.NextPageToken);
                });
}
```

## Consuming ListFeed in XAML with Pagination

ListView automatically supports pagination:

```xml
<ListView ItemsSource="{Binding People}">
    <ListView.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

The ListView will automatically call `LoadMoreItemsAsync` as the user scrolls.

## Awaiting ListFeed in Code

```csharp
public async ValueTask LogPeople(CancellationToken ct)
{
    // Await the list feed to get current items
    var people = await People;
    foreach (var person in people)
    {
        Console.WriteLine($"{person.FirstName} {person.LastName}");
    }
}
```

## Converting Feed to ListFeed

```csharp
public partial record ProductModel(IProductService Service)
{
    // Regular feed returning a list
    private IFeed<IImmutableList<Product>> ProductsFeed => 
        Feed.Async(Service.GetProductsAsync);
    
    // Convert to ListFeed
    public IListFeed<Product> Products => 
        ProductsFeed.AsListFeed();
}
```

## Best Practices

1. **Return IImmutableList<T>** from services for immutable snapshots
2. **Use FeedView** for proper loading/error/empty state handling
3. **Use Where** for client-side filtering
4. **Use SelectAsync with AsListFeed** for server-side filtering
5. **Use PaginatedAsync** for large datasets
6. **Use ListState** instead when you need to edit the collection
7. **Bind Data property** when using FeedView with lists
