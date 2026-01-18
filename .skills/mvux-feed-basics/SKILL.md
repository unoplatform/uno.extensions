---
name: mvux-feed-basics
description: Create and use IFeed<T> for async data in MVUX. Use when loading data from services, creating reactive data sources, or transforming async data with Select/Where operators.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# MVUX Feed Basics

Feeds (`IFeed<T>`) are the core reactive primitive in MVUX for representing asynchronous, read-only data streams.

## Prerequisites

- Add `MVUX` to `<UnoFeatures>` in your `.csproj`

```xml
<UnoFeatures>MVUX</UnoFeatures>
```

## What is a Feed?

An `IFeed<T>` represents:
- An asynchronous data source
- Built-in loading/error/none states
- Read-only (stateless) data flow
- Automatic refresh capability

## Creating Feeds

### From Async Method

```csharp
using Uno.Extensions.Reactive;

public partial record ProductModel(IProductService Service)
{
    // Single value from async method
    public IFeed<Product> CurrentProduct => 
        Feed.Async(Service.GetCurrentProductAsync);
}
```

### From Async Method with Cancellation

```csharp
public partial record ProductModel(IProductService Service)
{
    public IFeed<Product> CurrentProduct => 
        Feed.Async(async ct => await Service.GetProductAsync(ct));
}
```

### From IAsyncEnumerable (Push-based)

```csharp
public partial record StockModel(IStockService Service)
{
    // Continuously updated data
    public IFeed<decimal> StockPrice => 
        Feed.AsyncEnumerable(Service.GetPriceUpdates);
}
```

### With Refresh Signal

```csharp
public partial record ProductModel(IProductService Service)
{
    // External refresh trigger
    public Signal RefreshSignal { get; } = new();
    
    public IFeed<Product> CurrentProduct => 
        Feed.Async(Service.GetCurrentProductAsync, refresh: RefreshSignal);
}
```

## Feed Operators

### Select (Transform)

```csharp
public partial record WeatherModel(IWeatherService Service)
{
    public IFeed<WeatherInfo> Weather => 
        Feed.Async(Service.GetWeatherAsync);
    
    // Transform the feed value
    public IFeed<string> TemperatureDisplay => 
        Weather.Select(w => $"{w.Temperature}°C");
}
```

### SelectAsync (Async Transform)

```csharp
public partial record UserModel(IUserService UserService, IProfileService ProfileService)
{
    public IFeed<User> User => 
        Feed.Async(UserService.GetCurrentUserAsync);
    
    // Async transformation
    public IFeed<Profile> UserProfile => 
        User.SelectAsync(async (user, ct) => 
            await ProfileService.GetProfileAsync(user.Id, ct));
}
```

### Where (Filter)

```csharp
public partial record OrderModel(IOrderService Service)
{
    public IFeed<Order> CurrentOrder => 
        Feed.Async(Service.GetCurrentOrderAsync);
    
    // Filter: produces None if condition not met
    public IFeed<Order> CompletedOrder => 
        CurrentOrder.Where(order => order.Status == OrderStatus.Completed);
}
```

### WhereNotNull

```csharp
public IFeed<Product> Product => 
    Feed.Async(Service.GetProductAsync).WhereNotNull();
```

## Consuming Feeds in XAML

### Direct Binding

```xml
<!-- Binds to the feed value directly -->
<TextBlock Text="{Binding CurrentProduct.Name}" />
```

### With FeedView (Recommended)

```xml
<mvux:FeedView Source="{Binding CurrentProduct}">
    <DataTemplate>
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>
</mvux:FeedView>
```

## Consuming Feeds in Code

### Await Current Value

```csharp
public async ValueTask ProcessProduct(CancellationToken ct)
{
    // Await the feed to get current value
    var product = await CurrentProduct;
    if (product is not null)
    {
        // Process product
    }
}
```

### React to Changes with ForEach

```csharp
public ProductModel(IProductService service)
{
    Service = service;
    
    // Subscribe to feed changes
    CurrentProduct.ForEach(OnProductChanged);
}

private async ValueTask OnProductChanged(Product? product, CancellationToken ct)
{
    // React to product changes
}
```

## Combining Feeds

### Using SelectAsync with Dependencies

```csharp
public partial record SearchModel(ISearchService Service)
{
    public IState<string> SearchTerm => State<string>.Empty(this);
    
    // Feed depends on state
    public IFeed<SearchResults> Results => 
        SearchTerm.SelectAsync(async (term, ct) => 
            await Service.SearchAsync(term, ct));
}
```

### Using Feed.Combine

```csharp
public partial record DashboardModel(IUserService Users, IOrderService Orders)
{
    public IFeed<User> CurrentUser => Feed.Async(Users.GetCurrentAsync);
    public IFeed<Order> LatestOrder => Feed.Async(Orders.GetLatestAsync);
    
    // Combine multiple feeds
    public IFeed<DashboardData> Dashboard => 
        Feed.Combine(CurrentUser, LatestOrder, 
            (user, order) => new DashboardData(user, order));
}
```

## Feed with Parameters

```csharp
public partial record ProductDetailModel(IProductService Service)
{
    // Parameter state
    public IState<string> ProductId => State<string>.Empty(this);
    
    // Feed reloads when ProductId changes
    public IFeed<Product> Product => 
        ProductId.SelectAsync(async (id, ct) => 
            await Service.GetProductAsync(id, ct));
}
```

## Error Handling

Errors in feeds are captured automatically and exposed through the `FeedView.ErrorTemplate`:

```xml
<mvux:FeedView Source="{Binding CurrentProduct}">
    <mvux:FeedView.ValueTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Data.Name}" />
        </DataTemplate>
    </mvux:FeedView.ValueTemplate>
    <mvux:FeedView.ErrorTemplate>
        <DataTemplate>
            <StackPanel>
                <TextBlock Text="Error loading product" />
                <Button Content="Retry" Command="{Binding Refresh}" />
            </StackPanel>
        </DataTemplate>
    </mvux:FeedView.ErrorTemplate>
</mvux:FeedView>
```

## Best Practices

1. **Use Feed.Async** for one-time async data loading
2. **Use Feed.AsyncEnumerable** for push-based streaming data
3. **Use SelectAsync** when transformations require async operations
4. **Always handle error states** in UI with ErrorTemplate
5. **Use CancellationToken** in all async operations
6. **Prefer FeedView** over direct binding for full state handling
