---
uid: Uno.Extensions.Navigation.HowToDefineRoutes
---

# How-To: Define Routes

Routes provide an easy and dynamic way of navigating through your app, either via code-behind or XAML. They are particularly useful when your application has a high degree of complexity in terms of navigation, especially when dealing with nested pages using TabBars and NavigationViews. In such cases, there are multiple levels of navigation rather than just one level, as in an application that simply navigates between, for instance, a Welcome page, Login page, Main page, and Second page.

This topic walks through the process of defining routes.

## Step-by-step

> [!IMPORTANT]
> This guide assumes you used the template wizard or `dotnet new unoapp` to create your solution. If not, it is recommended that you follow the [Creating an application with Uno.Extensions article](xref:Uno.Extensions.HowToGettingStarted) to create an application from the template.

### Understanding routes

In a new app with Navigation, you'll find some pre-set views: `Shell`, `MainPage`, and `SecondPage`. `Shell` acts as the main frame where views are set and navigation begins. When the app launches, it opens `MainPage`, which then leads to `SecondPage`.

So if we take a look at the `RegisterRoutes` method in the `App.xaml.cs` file, we can see how these pages are organized in terms of routes:

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
                new ("Main", View: views.FindByViewModel<MainViewModel>()),
                new ("Second", View: views.FindByViewModel<SecondViewModel>()),
            ]
        )
    );
}
```

Firstly, we see that the views are registered using a `ViewMap` object. This object is also responsible for associating each View with its corresponding ViewModel. Next, we observe that the routes are being registered. Here, we can see that "Main" and "Second" are nested pages within "Shell", and both are at the same level. So if we take a look at the flow of the app, `ShellViewModel` opens `MainPage`, which in turn contains a button that navigates to `SecondPage`.

### ViewMap

When registering routes, we can take advantage of the `ViewMap` object and its variations `DataViewMap` and `ResultDataViewMap`, to correlate Views with ViewModels, specify the type of parameters ViewModels may take, and specify the type of return coming from a navigation.

Let's explore each of these arguments and how to effectively implement them:

1. **View** - The type of the View being registered. Example:

    ```csharp
    new ViewMap<MainPage>()
    ```

1. **ViewModel** - The type of the ViewModel being associated with the View. Example:

    ```csharp
    new ViewMap<MainPage, MainViewModel>()
    ```

    This correlation between View and ViewModel allows navigation through ViewModels. For example, when navigating to `MainPage` the following call could be made:

    ```csharp
     _navigator.NavigateViewModelAsync<MainViewModel>(this);
     ```

1. **Data** - In addition to associating Views with ViewModels, the Data type can also be associated with ViewModels. This Data type will be injected into the ViewModel constructor when an instance is created. To achieve this, we use the `DataViewMap` object instead of `ViewMap`.

    For example, let's say we have a `Product` class that holds product information, and we use `ProductDetailPage` to display the product details. We can register it as follows:

    ```csharp
    new DataViewMap<ProductDetailPage, ProductDetailViewModel, Product>()
    ```

    This allows navigation through data, for example when navigating to `ProductDetailPage` the following call could be made:

    ```csharp
    // Where `myProduct` is of type `Product`
    _navigator.NavigateDataAsync(this, myProduct);
    ```

    > [!NOTE]
    > In order to achieve this, ensure that your ViewModel constructor accepts the data type as a parameter.

1. **ResultData** - Defines an association between the view and the type of data being requested. For example, if you're navigating to a page that has a list of products and that page should return a product that was selected by the user you can achieve it by associating the `Product` type to the View and ViewModel using `ResultDataViewMap`:

    ```csharp
    new ResultDataViewMap<ProductsPage, ProductsViewModel, Product>()
    ```

    Then, when navigating to the `ProductsPage` and requesting a product from this navigation you can do:

    ```csharp
    public async Task GoToProductsPage()
    {
        var product = await _navigator.GetDataAsync<Product>(this);
    }
    ```

1. **FromQuery** - Used to convert a query parameter into entities when using deep linking.

    For example, let's say you have a route called "Product" which is associated with the `ProductDetailPage`. When navigating through a deep linking, you can reach that page by specifying the route name and the product you want to show by providing its ID. The way of converting the ID provided by the query string to a `Product` can be achieved with the `FromQuery` parameter, as follows:

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

1. **ToQuery** - Used to convert entities into a query parameter.

    Following the same logic of the previous example, you can use the `ToQuery` parameter to provide the logic to convert the `Product` into a query string:

    ```csharp
    new DataViewMap<ProductDetailPage, ProductDetailViewModel, Product>(
        ToQuery: product => new Dictionary<string, string>
        {
            { nameof(Product.Id), $"{product.Id}" }
        }
    )
    ```

> [!NOTE]
> `FromQuery` and `ToQuery` are only available when using `DataViewMap`.

### RouteMap

RouteMaps are especially useful for managing complex navigation systems, including those with nested views. When defining routes, we utilize the `RouteMap` object. This object's constructor takes six arguments. Let's explore each of these arguments and how to effectively implement them:

#### Path

The first parameter is **Path**, serving as the identifier for the route. When navigating within your app using routes, this is the reference you'll use to reach the corresponding view.

> [!IMPORTANT]
> When creating route names, please use only alphanumeric characters (letters and numbers). Avoid using special characters such as punctuation marks, symbols, or spaces. Using special characters may lead to incorrect or invalid navigation.

When defining route names, it's crucial to choose names carefully to avoid potential conflicts and errors. Certain names, such as "List", "Grid", or "Page" could unintentionally resolve to existing control, element, or class names within the application. For example, a route named "List" might be mistakenly resolved as a "ListView" class, leading to an incorrect path and causing errors in navigation. To avoid these issues, it's best to avoid using common names that might conflict with existing control or class names in the application. Here are some examples of names to avoid:

**Names that could be mistaken for controls, elements, or components:**

- Scroll
- List
- Grid
- Tree
- Web
- Navigation
- Content
- User
- Items
- Menu

**Suffixes that could possibly resolve in an existing class:**

- Page
- Model
- ViewModel
- Service
- Helper
- Converter
- Manager
- Handler
- Exception
- Extension
- Settings

#### View

The **View** parameter refers to the view associated with the route. The view must be registered beforehand within the `IViewRegistry`, and you can establish the association using the `FindByViewModel` method. Note in the following example how we add a route for a new page called `LoginPage`:

```csharp
protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<LoginPage, LoginViewModel>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new ("Login", View: views.FindByViewModel<LoginViewModel>())
            ]
        )
    );
}
```

Note that adding a view is not mandatory. You can specify a route without a view. For example, if you want to add routes for the content of a `TabBar`, you can define the routes without the view, and in your markup file, you can use the attached property `uen:Region.Name` to bind the content view with the defined route.

Given the following routes:

```csharp
// Omitted for brevity
new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
    Nested:
    [
        new ("ForYouTab"),
        new ("FavoritesTab")
    ]
)
```

We can specify the content view using the `uen:Region.Name` to link it to the given route name:

```xml
xmlns:uen="using:Uno.Extensions.Navigation.UI"

<Grid uen:Region.Attached="True">
    <utu:TabBar uen:Region.Attached="True">
        <utu:TabBarItem Content="For You"
                        uen:Region.Name="ForYouTab" />
        <utu:TabBarItem Content="Favorites"
                        uen:Region.Name="FavoritesTab"/>
    </utu:TabBar>
    <Grid uen:Region.Attached="True"
        uen:Region.Navigator="Visibility">
        <ListView uen:Region.Name="ForYouTab"
                ItemsSource="{Binding Items}">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{x:Bind Name}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
        <Grid uen:Region.Name="FavoritesTab">
            <TextBlock Text="Favorites go here" />
        </Grid>
    </Grid>
</Grid>
```

In that example, we used the `uen:Region.Name` attached property on the `ListView` to associate it with the route named "ForYouTab" as defined in the `RouteMap`. Similarly, the `Grid` was linked to the "FavoritesTab" route using the same attached property.

#### Nested

The **Nested** property allows you to define hierarchical routes. It creates a parent-child relationship between routes. This helps organize navigation structure and manage complex navigation scenarios more effectively, for example when using `TabBar` or `NavigationView`. In the following example `MainPage` has two nested routes, that represents tabs within `MainPage`: "ForYouTab" and "FavoritesTab":

```csharp
protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<LoginPage, LoginViewModel>(),
        new ViewMap<MainPage, MainViewModel>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new ("Login", View: views.FindByViewModel<LoginViewModel>()),
                new ("Main", View: views.FindByViewModel<MainViewModel>(),
                    Nested:
                    [
                        new ("ForYouTab"),
                        new ("FavoritesTab")
                    ]
                )
            ]
        )
    );
}
```

#### IsDefault

**IsDefault** will make the navigator automatically shows that route when dealing with nested views. For example, imagine a scenario where you are defining routes within a `NavigationView`, such as nested routes inside a `MainPage`. You can set one of these routes as the default by setting `IsDefault: true`. This ensures that the specified route is automatically navigated to when the page is displayed.

```csharp
// Omitted for brevity
new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
    Nested:
    [
        new ("ForYouTab", IsDefault: true),
        new ("FavoritesTab")
    ]
)
```

#### DependsOn

**DependsOn** enables you to establish a dependency between two views. This argument expects a route name and ensures that when you navigate to a view with dependencies, the dependent view will be navigated to first before opening the requested view. This is especially useful when using deep linking to navigate through pages. In the following example we add a new page called `ProductsPage` and we set this page as dependent of `MainPage`:

```csharp
protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        // Omitted for brevity
        new ViewMap<ProductsPage, ProductsViewModel>()
    );

    routes
        .Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    // Omitted for brevity
                    new ("Main", View: views.FindByViewModel<MainViewModel>()),
                    new ("Products", View: views.FindByViewModel<ProductsViewModel>(), DependsOn: "Main"),
                ]
            )
        );
}
```

#### Init

**Init** allows you to customize the navigation request, enabling specific actions before displaying the associated view. For example, you can override the navigation request if the user is not logged in:

```csharp
new ("Login", View: views.FindByViewModel<LoginViewModel>()),
new ("Main", View: views.FindByViewModel<MainViewModel>(),
    Init: (request) => 
    {
        // Check if the user is logged in
        if (!User.IsLoggedIn())
        {
            // Redirect to the Login page if the user is not logged in
            request = request with { Route = Route.PageRoute("Login") };
        }
        return request;
    }),
```
