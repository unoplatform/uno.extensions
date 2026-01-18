---
name: uno-navigation-routes
description: Define and register navigation routes in Uno Platform applications using ViewMap, DataViewMap, ResultDataViewMap, and RouteMap. Use when setting up view-viewmodel associations, data-based navigation, result-based navigation, nested routes, route dependencies, and route initialization logic.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Navigation Routes Registration

This skill covers defining and registering navigation routes in Uno Platform applications.

## ViewMap Types

### Basic ViewMap

Associates a View with a ViewModel:

```csharp
views.Register(
    new ViewMap<MainPage, MainViewModel>(),
    new ViewMap<SecondPage, SecondViewModel>()
);
```

### ViewMap Without ViewModel

For views without viewmodels:

```csharp
new ViewMap(View: typeof(WelcomePage))
```

### ViewMap With Only ViewModel

For shell or container viewmodels:

```csharp
new ViewMap(ViewModel: typeof(ShellViewModel))
```

### DataViewMap

Associates a View with a ViewModel and input data type. Data is injected into ViewModel constructor:

```csharp
new DataViewMap<ProductDetailPage, ProductDetailViewModel, Product>()
```

ViewModel receives data via constructor:

```csharp
public class ProductDetailViewModel
{
    public ProductDetailViewModel(Product product)
    {
        // Use product data
    }
}
```

### DataViewMap with Query Conversion

Convert between data objects and URL query parameters for deep linking:

```csharp
new DataViewMap<ProductDetailPage, ProductDetailViewModel, Product>(
    ToQuery: product => new Dictionary<string, string>
    {
        { nameof(Product.Id), $"{product.Id}" }
    },
    FromQuery: async (sp, query) =>
    {
        var productService = sp.GetRequiredService<IProductService>();
        var id = query[nameof(Product.Id)];
        return await productService.GetById(id, default);
    }
)
```

### ResultDataViewMap

For views that return data (like pickers or selectors):

```csharp
new ResultDataViewMap<ProductsPage, ProductsViewModel, Product>()
```

Request data from navigation:

```csharp
var product = await _navigator.GetDataAsync<Product>(this);
```

## RouteMap Configuration

### Basic RouteMap

```csharp
routes.Register(
    new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
        Nested:
        [
            new RouteMap("Main", View: views.FindByViewModel<MainViewModel>()),
            new RouteMap("Second", View: views.FindByViewModel<SecondViewModel>())
        ]
    )
);
```

### RouteMap Properties

| Property | Description |
|----------|-------------|
| `Path` | Route identifier (alphanumeric only, no special characters) |
| `View` | Associated ViewMap from IViewRegistry |
| `IsDefault` | Auto-navigate to this route when parent is displayed |
| `DependsOn` | Ensure another route is navigated first |
| `Init` | Customize or redirect navigation request |
| `Nested` | Child routes for hierarchical navigation |

### Nested Routes

Create hierarchical route structures for TabBar or NavigationView:

```csharp
new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
    Nested:
    [
        new RouteMap("ForYouTab", IsDefault: true),
        new RouteMap("FavoritesTab"),
        new RouteMap("ProfileTab")
    ]
)
```

### Default Route

Set a default nested route that auto-navigates:

```csharp
new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
    Nested:
    [
        new RouteMap("Home", View: views.FindByView<HomePage>(), IsDefault: true),
        new RouteMap("Settings", View: views.FindByView<SettingsPage>())
    ]
)
```

### Route Dependencies

Ensure dependent routes are navigated first (useful for deep linking):

```csharp
new RouteMap("Products", View: views.FindByViewModel<ProductsViewModel>(), DependsOn: "Main")
```

When navigating to "Products", "Main" will be navigated first.

### Route Initialization (Redirect Logic)

Customize or redirect navigation requests:

```csharp
new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
    Init: (request) => 
    {
        if (!User.IsLoggedIn())
        {
            // Redirect to Login if user is not authenticated
            request = request with { Route = Route.PageRoute("Login") };
        }
        return request;
    })
```

## Routes Without Views

Define routes without views for inline content (controlled by `Region.Name`):

```csharp
new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
    Nested:
    [
        new RouteMap("ForYouTab"),  // No view - uses Region.Name in XAML
        new RouteMap("FavoritesTab")
    ]
)
```

XAML binds content to route:

```xml
<Grid uen:Region.Name="ForYouTab">
    <TextBlock Text="For You Content" />
</Grid>
```

## Complete Registration Example

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<MainPage, MainViewModel>(),
        new ViewMap<LoginPage, LoginViewModel>(),
        new DataViewMap<ProductDetailPage, ProductDetailViewModel, Product>(
            ToQuery: p => new Dictionary<string, string> { { "Id", p.Id.ToString() } },
            FromQuery: async (sp, q) => await sp.GetRequiredService<IProductService>()
                .GetById(q["Id"], default)
        ),
        new ResultDataViewMap<ProductsPage, ProductsViewModel, Product>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new RouteMap("Login", View: views.FindByViewModel<LoginViewModel>()),
                new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
                    Init: request => !IsLoggedIn ? request with { Route = Route.PageRoute("Login") } : request,
                    Nested:
                    [
                        new RouteMap("Home", IsDefault: true),
                        new RouteMap("Products", View: views.FindByViewModel<ProductsViewModel>()),
                        new RouteMap("ProductDetail", View: views.FindByViewModel<ProductDetailViewModel>())
                    ]
                )
            ]
        )
    );
}
```

## Route Naming Best Practices

**Avoid these names** (may conflict with controls/classes):
- Scroll, List, Grid, Tree, Web, Navigation, Content, User, Items, Menu
- Page, Model, ViewModel, Service, Helper, Converter, Manager, Handler

**Good route names:**
- Home, Products, Settings, Profile, Search, Details
- ProductList, UserProfile, OrderHistory
