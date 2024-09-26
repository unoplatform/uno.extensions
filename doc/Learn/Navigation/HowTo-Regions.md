---
uid: Uno.Extensions.Navigation.Regions
---
# How-To: Define Regions

A Region is used to link specific sectors of a view to individual items on a navigation control within the same page. This helps in managing and organizing navigation within different sections of a view, ensuring that each section can independently display and navigate through content. Regions are usually used to manage the navigation of pages and to display or hide content with controls like `NavigationView` and `TabBars`.

> [!IMPORTANT]
> When working with `NavigationView` or `TabBar`, it's crucial not to define them within `ShellView`. Instead, these controls should be defined on your `MainPage` or another appropriate page in your application.
>
> [!NOTE]
> If an app only uses basic navigation between pages without any nested views or pages, it is not necessary to register routes. In such cases, the default `Frame` and navigator are sufficient to manage the navigation, simplifying the setup and reducing the need for additional configuration.

## Properties in the Region Class

1. **`Region.Attached`**:
   - **Description**: This property is used to mark a sector of the view as a region. It links the navigational control with the container provided to render content.
   - **Example**: `<Grid uen:Region.Attached="True">`

1. **`Region.Navigator`**:
   - **Description**: Defines the navigation behavior for the region, determining how the region responds to navigation changes.
   - **Values**: `Visibility`.
   - **Example**: `uen:Region.Navigator="Visibility"`

1. **`Region.Name`**:
   - **Description**: Assigns a name to the region, useful for identifying and referencing the region in code.
   - **Example**: `uen:Region.Name="Main"`

## How to use Regions

Regions are particularly useful with navigational controls, such as `NavigationView` and `TabBar` for showing or hiding, and opening or closing content and pages. In these cases, it's recommended to have a `Grid` element as the parent of the navigational control, which we will refer to as the "Parent Grid" in this guide. Additionally, it is essential to define a `Grid` that will hold the content to be displayed based on the selected `NavigationViewItem` or `TabBarItem`, and we will call it a "Content Grid".

Then it's needed to add the `Region.Attached="True"` attached property to them, the navigational control and the Content `Grid` can be linked.

> [!IMPORTANT]
> If the navigational control used is a `TabBar`, the "Parent Grid" must also have the `Region.Attached="True"` attached property.

See the example:

```xml
<Grid uen:Region.Attached="True">
   <Grid uen:Region.Attached="True" />
   <utu:TabBar uen:Region.Attached="True">
      <!-- Items -->
   </utu:TabBar>
</Grid>
```

It's also important to inform the type of navigator that the Content `Grid` will use to display the content, for example, we can add the `Region.Navigator` to the `Grid` and set it to `Visibility`, then the navigator will be able to manipulate the content visibility in order to show the requested content.

```diff
<Grid uen:Region.Attached="True">
   <Grid uen:Region.Attached="True"
+        uen:Region.Navigator="Visibility" />
   <utu:TabBar uen:Region.Attached="True">
      <!-- Items -->
   </utu:TabBar>
</Grid>
```

The next important thing to do is to set the region names to associate the navigational control items with the content to be displayed. But before we give an example, we need to consider that there are two ways of setting the content in the Content `Grid`:

- 1. Set the content directly inside the Content `Grid`. Then we can map each content with a region name using `Region.Name`:

   ```xml
   <Grid uen:Region.Attached="True"
         uen:Region.Navigator="Visibility">
      <Grid uen:Region.Name="One" Visibility="Collapsed">
         <TextBlock HorizontalAlignment="Center"
                  VerticalAlignment="Center"
                  FontSize="24"
                  Text="One" />
      </Grid>
      <Grid uen:Region.Name="Two" Visibility="Collapsed">
         <TextBlock HorizontalAlignment="Center"
                  VerticalAlignment="Center"
                  FontSize="24"
                  Text="Two" />
      </Grid>
      <Grid uen:Region.Name="Three" Visibility="Collapsed">
         <StackPanel>
            <TextBlock HorizontalAlignment="Center"
                     VerticalAlignment="Center"
                     FontSize="24"
                     Text="Three" />
         </StackPanel>
      </Grid>
   </Grid>
   ```

   For this case, we can then associate the navigational control items with the contents by adding the same region names:

   ```xml
   <utu:TabBar.Items>
      <utu:TabBarItem uen:Region.Name="One" Content="Tab One" />
      <utu:TabBarItem uen:Region.Name="Two" Content="Tab Two" />
      <utu:TabBarItem uen:Region.Name="Three" Content="Tab Three" />
   </utu:TabBar.Items>
   ```

   Then when a `TabBarItem` is selected the navigator will change the corresponding content `Visibility` to `True` and it will be displayed.

- 1. Another method of setting the content is by using the registered route names associated with a view. For example, if you have three views registered as routes with the names "Products", "Favorites", and "Deals", you can simply configure the navigational control items to correspond with these route names:

   ```csharp
   new ("Main", View: views.FindByView<MainPage>(),
      Nested:
      [
         new ("Products", View: views.FindByView<ProductsContentControl>(), IsDefault: true),
         new ("Favorites", View: views.FindByView<FavoritesContentControl>()),
         new ("Deals", View: views.FindByView<DealsContentControl>())
      ]
   )
   ```

   ```xml
   <utu:TabBar.Items>
      <utu:TabBarItem uen:Region.Name="Products" Content="Tab One" />
      <utu:TabBarItem uen:Region.Name="Favorites" Content="Tab Two" />
      <utu:TabBarItem uen:Region.Name="Deals" Content="Tab Three" />
   </utu:TabBar.Items>
   ```

   When a `TabBarItem` is selected, the navigator will display the page corresponding to the route name associated with the Region name defined in the `TabBarItem`.

Additionally, you can explore our detailed step-by-step tutorials on implementing these navigational controls in your app:

- [How-To: Navigate using a TabBar](xref:Uno.Extensions.Navigation.Advanced.TabBar)
- [How-To: Navigate using a NavigationView](xref:Uno.Extensions.Navigation.Advanced.NavigationView)

## Navigators

In *Uno.Extensions* navigation begins when a `Frame` is injected into the `ShellView`. This `Frame` acts as the main container for navigating between pages such as `WelcomePage`, `LoginPage`, and `MainPage`. The `Frame` has an associated navigator that handles the transitions between these pages.

When there are nested views or pages, such as tabs defined through "Regions" within a `MainPage` for example, a second `Frame` is injected in the provided container to render these tabs' content. This second `Frame` also has its own navigator, which manages navigation within the specific region of the tabs. Therefore, if a new page is opened from one of the tabs, this navigation is handled by the navigator associated with that specific region. This setup allows for hierarchical and organized navigation, where each region or tab maintains independent control over its page transitions, ensuring smooth and structured navigation within the application.
