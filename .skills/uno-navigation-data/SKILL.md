---
name: uno-navigation-data
description: Pass and receive data during navigation in Uno Platform applications. Use when implementing data transfer between pages, data-based routing, constructor injection of navigation data, requesting data from pickers/selectors, and round-trip data scenarios.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Navigation Data Passing

This skill covers passing and receiving data during navigation in Uno Platform.

## Data Passing Methods

### 1. Pass Data to Another Page

**Route Registration:**
```csharp
new DataViewMap<ProductDetailPage, ProductDetailViewModel, Product>()
```

**Navigation with Data:**
```csharp
var product = new Product("Widget", 29.99);
await _navigator.NavigateViewModelAsync<ProductDetailViewModel>(this, data: product);
```

**ViewModel Receives Data (Constructor Injection):**
```csharp
public class ProductDetailViewModel
{
    public string Name { get; }
    public decimal Price { get; }

    public ProductDetailViewModel(Product product)
    {
        Name = product.Name;
        Price = product.Price;
    }
}
```

### 2. Navigate by Data Type

Let the framework determine the destination based on data type:

```csharp
await _navigator.NavigateDataAsync(this, data: myProduct);
```

The navigation system uses `DataViewMap` registrations to resolve the appropriate route.

### 3. Pass Data in XAML

```xml
<Button Content="View Details"
        uen:Navigation.Request="ProductDetail"
        uen:Navigation.Data="{Binding SelectedProduct}" />
```

### 4. List Item Selection with Data

Empty `Navigation.Request` enables automatic data-based routing:

```xml
<ListView ItemsSource="{Binding Products}"
          uen:Navigation.Request="">
    <ListView.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

Selected item is passed as navigation data automatically.

## Receiving Data in ViewModels

### Constructor Injection

The most common pattern - data is injected into the ViewModel constructor:

```csharp
public class ProductDetailViewModel
{
    private readonly Product _product;

    public ProductDetailViewModel(Product product)
    {
        _product = product;
        // Initialize with product data
    }
}
```

### Polymorphic Data Routing

Route to different pages based on data subtype:

**Route Registration:**
```csharp
new DataViewMap<BookDetailPage, BookDetailViewModel, Book>(),
new DataViewMap<MovieDetailPage, MovieDetailViewModel, Movie>()
```

Where `Book` and `Movie` inherit from a base type:

```csharp
public record MediaItem(string Title);
public record Book(string Title, string Author) : MediaItem(Title);
public record Movie(string Title, string Director) : MediaItem(Title);
```

**List with Mixed Types:**
```csharp
public MediaItem[] Items { get; } =
[
    new Book("The Great Gatsby", "F. Scott Fitzgerald"),
    new Movie("Inception", "Christopher Nolan")
];
```

Navigation automatically routes to the correct page based on item type.

## Requesting Data (Round-Trip)

### GetDataAsync - Simple Result

Navigate to a picker and get the selected result:

```csharp
public async Task SelectProduct()
{
    var product = await _navigator.GetDataAsync<Product>(this);
    
    if (product is not null)
    {
        SelectedProduct = product;
    }
}
```

**Registration for Result:**
```csharp
new ResultDataViewMap<ProductPickerPage, ProductPickerViewModel, Product>()
```

### NavigateBackWithResultAsync

Return data when navigating back:

```csharp
// In the picker/selector ViewModel
public async ValueTask SelectItem(Product product)
{
    await _navigator.NavigateBackWithResultAsync(this, data: product);
}
```

### XAML-Based Return

Use `-` to navigate back with selected item:

```xml
<ListView ItemsSource="{Binding Products}"
          uen:Navigation.Request="-">
    <ListView.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

## Two-Way Data Binding

For scenarios like filters where data is updated and returned:

**XAML:**
```xml
<Button Content="Edit Filters"
        uen:Navigation.Request="!Filter"
        uen:Navigation.Data="{Binding Filter.Value, Mode=TwoWay}" />
```

**Filter ViewModel:**
```csharp
public class FilterViewModel
{
    private readonly INavigator _navigator;
    private SearchFilter _filter;

    public FilterViewModel(INavigator navigator, SearchFilter filter)
    {
        _navigator = navigator;
        _filter = filter;
    }

    public async ValueTask ApplyFilter()
    {
        // Update filter and return
        await _navigator.NavigateBackWithResultAsync(this, data: _filter);
    }
}
```

## Deep Linking with Data Conversion

Convert data to/from URL query parameters:

**Registration with Converters:**
```csharp
new DataViewMap<ProductDetailPage, ProductDetailViewModel, Product>(
    ToQuery: product => new Dictionary<string, string>
    {
        { nameof(Product.Id), product.Id.ToString() }
    },
    FromQuery: async (serviceProvider, query) =>
    {
        var productService = serviceProvider.GetRequiredService<IProductService>();
        var id = query[nameof(Product.Id)];
        return await productService.GetById(id, default);
    }
)
```

This enables:
- Deep linking: `myapp://products/detail?Id=123`
- Browser URL updates (WebAssembly)
- State restoration

## Complete Example: Master-Detail with Data

**App.xaml.cs:**
```csharp
views.Register(
    new ViewMap<ProductsPage, ProductsViewModel>(),
    new DataViewMap<ProductDetailPage, ProductDetailViewModel, Product>()
);

routes.Register(
    new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
        Nested:
        [
            new RouteMap("Products", View: views.FindByViewModel<ProductsViewModel>()),
            new RouteMap("ProductDetail", View: views.FindByViewModel<ProductDetailViewModel>())
        ]
    )
);
```

**ProductsPage.xaml:**
```xml
<ListView ItemsSource="{Binding Products}"
          uen:Navigation.Request="ProductDetail">
    <ListView.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

**ProductDetailViewModel.cs:**
```csharp
public class ProductDetailViewModel
{
    public Product Product { get; }

    public ProductDetailViewModel(Product product)
    {
        Product = product;
    }
}
```

## Best Practices

1. **Use `DataViewMap`** when navigation always requires data of a specific type

2. **Use constructor injection** to receive navigation data in ViewModels

3. **Use `ResultDataViewMap`** for pickers/selectors that return data

4. **Implement `ToQuery`/`FromQuery`** for deep linking support

5. **Use `Mode=TwoWay`** for round-trip data scenarios in XAML

6. **Handle null results** from `GetDataAsync` - user may cancel
