---
name: mvux-pagination
description: Implement paginated and infinite scrolling lists in MVUX. Use when loading large datasets incrementally, implementing cursor-based APIs, or enabling infinite scroll in ListView/GridView.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# MVUX Pagination

MVUX provides built-in support for paginated lists, enabling infinite scrolling with automatic page loading.

## Prerequisites

- Add `MVUX` to `<UnoFeatures>` in your `.csproj`

```xml
<UnoFeatures>MVUX</UnoFeatures>
```

## Index-based Pagination

For APIs that accept page size and start index:

### Service Interface

```csharp
public partial record Person(int Id, string FirstName, string LastName);

public interface IPeopleService
{
    ValueTask<IImmutableList<Person>> GetPeopleAsync(
        uint pageSize, 
        uint startIndex, 
        CancellationToken ct);
}
```

### Service Implementation

```csharp
public class PeopleService : IPeopleService
{
    public async ValueTask<IImmutableList<Person>> GetPeopleAsync(
        uint pageSize, 
        uint startIndex, 
        CancellationToken ct)
    {
        // Simulate API call
        await Task.Delay(500, ct);
        
        var allPeople = GetAllPeople(); // Your data source
        
        return allPeople
            .Skip((int)startIndex)
            .Take((int)pageSize)
            .ToImmutableList();
    }
}
```

### Model with Paginated Feed

```csharp
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Sources;

public partial record PeopleModel(IPeopleService Service)
{
    public IListFeed<Person> People => 
        ListFeed.PaginatedAsync<Person>(async (request, ct) =>
            await Service.GetPeopleAsync(
                request.DesiredSize ?? 20,
                request.CurrentCount,
                ct));
}
```

### XAML - Automatic Loading

```xml
<!-- ListView automatically triggers pagination on scroll -->
<ListView ItemsSource="{Binding People}">
    <ListView.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding FirstName}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

The ListView calls `LoadMoreItemsAsync` automatically as the user scrolls.

## Cursor-based Pagination

For APIs that return a "next page token":

### Service Interface

```csharp
public record Video(string Id, string Title);

public record VideoPage(
    IReadOnlyList<Video> Videos,
    string? NextPageToken);

public interface IVideoService
{
    Task<VideoPage> SearchVideosAsync(
        string query,
        string? pageToken,
        int pageSize,
        CancellationToken ct);
}
```

### Model with Cursor Pagination

```csharp
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Sources;

public partial record VideoSearchModel(IVideoService Service)
{
    public IState<string> SearchQuery => State<string>.Empty(this);
    
    public IListFeed<Video> Videos => 
        SearchQuery
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .SelectPaginatedByCursorAsync(
                firstPage: string.Empty,
                getPage: async (query, cursor, pageSize, ct) =>
                {
                    var page = await Service.SearchVideosAsync(
                        query,
                        string.IsNullOrEmpty(cursor) ? null : cursor,
                        pageSize ?? 20,
                        ct);
                    
                    return new PageResult<string, Video>(
                        page.Videos,
                        page.NextPageToken);
                });
}
```

### XAML

```xml
<StackPanel>
    <TextBox Text="{Binding SearchQuery, Mode=TwoWay}" 
             PlaceholderText="Search videos..." />
    
    <ListView ItemsSource="{Binding Videos}">
        <ListView.ItemTemplate>
            <DataTemplate>
                <TextBlock Text="{Binding Title}" />
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</StackPanel>
```

## Pagination with Parameters

Reload paginated feed when parameters change:

```csharp
public partial record ProductsModel(IProductService Service)
{
    // Filter criteria
    public IState<string> Category => State.Value(this, () => "all");
    
    // Paginated feed that resets when Category changes
    public IListFeed<Product> Products => 
        Category.SelectPaginatedAsync(
            async (category, request, ct) =>
                await Service.GetProductsAsync(
                    category,
                    request.DesiredSize ?? 20,
                    request.CurrentCount,
                    ct));
}
```

```xml
<ComboBox SelectedValue="{Binding Category, Mode=TwoWay}">
    <x:String>all</x:String>
    <x:String>electronics</x:String>
    <x:String>clothing</x:String>
</ComboBox>

<ListView ItemsSource="{Binding Products}">
    <!-- Items load fresh when category changes -->
</ListView>
```

## Refresh Paginated Feed

```xml
<mvux:FeedView Source="{Binding People}">
    <DataTemplate>
        <Grid>
            <ListView ItemsSource="{Binding Data}">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding FirstName}" />
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
            
            <Button Content="Refresh"
                    Command="{Binding Refresh}"
                    VerticalAlignment="Top"
                    HorizontalAlignment="Right" />
        </Grid>
    </DataTemplate>
</mvux:FeedView>
```

Refreshing reloads from page 0.

## Show Loading State

### With FeedView

```xml
<mvux:FeedView Source="{Binding People}">
    <mvux:FeedView.ProgressTemplate>
        <DataTemplate>
            <StackPanel HorizontalAlignment="Center">
                <ProgressRing IsActive="True" />
                <TextBlock Text="Loading..." />
            </StackPanel>
        </DataTemplate>
    </mvux:FeedView.ProgressTemplate>
    
    <DataTemplate>
        <ListView ItemsSource="{Binding Data}">
            <!-- Items template -->
        </ListView>
    </DataTemplate>
</mvux:FeedView>
```

## PageRequest Properties

The `PageRequest` object passed to your pagination method contains:

| Property | Type | Description |
|----------|------|-------------|
| `DesiredSize` | `uint?` | Requested page size (from ListView) |
| `CurrentCount` | `uint` | Number of items already loaded |

## Complete Example

### Model

```csharp
public partial record Product(string Id, string Name, decimal Price);

public partial record ShopModel(IProductService Service)
{
    public IState<string> SearchTerm => State<string>.Empty(this);
    public IState<string> SortBy => State.Value(this, () => "name");
    
    public IListFeed<Product> Products =>
        Feed.Combine(SearchTerm, SortBy)
            .SelectPaginatedAsync(async (search, sort, request, ct) =>
            {
                return await Service.SearchProductsAsync(
                    search,
                    sort,
                    request.DesiredSize ?? 25,
                    request.CurrentCount,
                    ct);
            });
}
```

### XAML

```xml
<Page x:Class="MyApp.ShopPage"
      xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    
    <Grid RowDefinitions="Auto,Auto,*">
        <!-- Search -->
        <TextBox Text="{Binding SearchTerm, Mode=TwoWay}"
                 PlaceholderText="Search products..." />
        
        <!-- Sort -->
        <ComboBox Grid.Row="1" 
                  SelectedValue="{Binding SortBy, Mode=TwoWay}">
            <x:String>name</x:String>
            <x:String>price-low</x:String>
            <x:String>price-high</x:String>
        </ComboBox>
        
        <!-- Paginated list -->
        <mvux:FeedView Grid.Row="2" Source="{Binding Products}">
            <DataTemplate>
                <ListView ItemsSource="{Binding Data}">
                    <ListView.ItemTemplate>
                        <DataTemplate>
                            <Grid ColumnDefinitions="*,Auto">
                                <TextBlock Text="{Binding Name}" />
                                <TextBlock Grid.Column="1" 
                                           Text="{Binding Price}" />
                            </Grid>
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
                    <TextBlock Text="No products found" 
                               HorizontalAlignment="Center" />
                </DataTemplate>
            </mvux:FeedView.NoneTemplate>
        </mvux:FeedView>
    </Grid>
</Page>
```

## Best Practices

1. **Use PaginatedAsync** for index-based APIs (offset/limit)
2. **Use SelectPaginatedByCursorAsync** for cursor/token-based APIs
3. **Return IImmutableList<T>** from your service methods
4. **Include reasonable default page size** (typically 20-50 items)
5. **Handle empty results** with NoneTemplate
6. **Refresh resets pagination** - loads from first page
7. **Combine with FeedView** for proper loading states
8. **Use Feed.Combine** when multiple parameters affect pagination
