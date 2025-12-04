---
uid: Uno.Extensions.Navigation.Walkthrough.DefineRoutes
title: Define Navigation Routes
tags: [uno, uno-platform, uno-extensions, navigation, routes, RouteMap, ViewMap, DataViewMap, ResultDataViewMap, nested-routes, hierarchical-routes, deep-linking, FromQuery, ToQuery, route-definition, route-registration, IViewRegistry, IRouteRegistry, FindByViewModel, FindByView, IsDefault, DependsOn, Init, route-path, route-hierarchy, navigation-structure, type-based-routing, data-injection, query-parameters, route-configuration]
---

# Define Navigation Routes

> **UnoFeatures:** `Navigation` (add to `<UnoFeatures>` in your `.csproj`)

## Register Navigation Views and Routes

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<MainPage, MainViewModel>(),
        new DataViewMap<SecondPage, SecondViewModel, Entity>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new("Main", View: views.FindByViewModel<MainViewModel>()),
                new("Second", View: views.FindByViewModel<SecondViewModel>()),
            ]
        )
    );
}
```

## Navigate with data

```csharp
new DataViewMap<ProductDetailPage, ProductDetailViewModel, Product>()
```

```csharp
_navigator.NavigateDataAsync(this, myProduct);
```

ViewModel constructor must accept the data type parameter.

## Request data from navigation

```csharp
new ResultDataViewMap<ProductsPage, ProductsViewModel, Product>()
```

```csharp
var product = await _navigator.GetDataAsync<Product>(this);
```

## Convert query parameters to data

```csharp
new DataViewMap<ProductDetailPage, ProductDetailViewModel, Product>(
    FromQuery: async (sp, query) =>
    {
        var productService = sp.GetRequiredService<IProductService>();
        var id = query[nameof(Product.Id)];
        return await productService.GetById(id, default);
    }
)
```

## Convert data to query parameters

```csharp
new DataViewMap<ProductDetailPage, ProductDetailViewModel, Product>(
    ToQuery: product => new Dictionary<string, string>
    {
        { nameof(Product.Id), $"{product.Id}" }
    }
)
```

## Define nested routes

```csharp
routes.Register(
    new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
        Nested:
        [
            new("Login", View: views.FindByViewModel<LoginViewModel>()),
            new("Main", View: views.FindByViewModel<MainViewModel>(),
                Nested:
                [
                    new("ForYouTab"),
                    new("FavoritesTab")
                ]
            )
        ]
    )
);
```

## Set default nested route

```csharp
new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
    Nested:
    [
        new("ForYouTab", IsDefault: true),
        new("FavoritesTab")
    ]
)
```

Automatically navigates to the default route when parent view is displayed.

## Redirect based on conditions

```csharp
new("Main", View: views.FindByViewModel<MainViewModel>(),
    Init: (request) => 
    {
        if (!User.IsLoggedIn())
        {
            request = request with { Route = Route.PageRoute("Login") };
        }
        return request;
    }),
```
