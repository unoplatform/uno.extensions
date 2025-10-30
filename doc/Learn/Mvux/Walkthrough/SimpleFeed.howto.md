---
uid: Uno.Extensions.Mvux.SimpleFeed.HowTo
---
# How to show data on the UI from a service with MVUX

**Goal:** display a list of items that comes from a feed (MVUX `Feed<T>`).

**Dependencies:**

* `Uno.Extensions`
* `Uno.Extensions.Reactive` (or the meta package that brings it)
* A view model using MVUX feed pattern

```csharp
// ViewModels/ProductsViewModel.cs
using Uno.Extensions.Reactive;
using System.Collections.Immutable;

public record Product(string Id, string Name);

public partial record ProductsViewModel
{
    public IFeed<ImmutableArray<Product>> Products { get; }

    public ProductsViewModel(IProductsService productsService)
    {
        Products = Feed.Async(async ct =>
        {
            var items = await productsService.GetProductsAsync(ct);
            return items.ToImmutableArray();
        });
    }
}
```

**XAML view:**

```xml
<Page
    x:Class="MyApp.ProductsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">

    <Grid>
        <!-- FeedView picks up IFeed<T> and renders for each state -->
        <mvux:FeedView Source="{Binding Products}">
            <mvux:FeedView.ValueTemplate>
                <ListView ItemsSource="{Binding}">
                    <ListView.ItemTemplate>
                        <DataTemplate x:DataType="local:Product">
                            <TextBlock Text="{x:Bind Name}" />
                        </DataTemplate>
                    </ListView.ItemTemplate>
                </ListView>
            </mvux:FeedView.ValueTemplate>
        </mvux:FeedView>
    </Grid>
</Page>
```

**What this shows:**

* `FeedView` understands MVUX `IFeed<T>`.
* When data is available → your ListView is shown.
* You don’t manage loading/error here.

---

## How to show loading while the feed runs

**Goal:** user sees a loading state before data shows.

```xml
<mvux:FeedView Source="{Binding Products}">
    <mvux:FeedView.LoadingTemplate>
        <Grid Padding="16">
            <ProgressRing IsActive="True" />
        </Grid>
    </mvux:FeedView.LoadingTemplate>

    <mvux:FeedView.ValueTemplate>
        <ListView ItemsSource="{Binding}">
            <!-- your item template -->
        </ListView>
    </mvux:FeedView.ValueTemplate>
</mvux:FeedView>
```

**Why:** MVUX feeds are async, so UI must declare what to show during *loading*.

---

## How to show an error from the feed

**Goal:** if the feed fails, show a message and a retry.

```xml
<mvux:FeedView Source="{Binding Products}">
    <mvux:FeedView.ErrorTemplate>
        <StackPanel Spacing="8" Padding="16">
            <TextBlock Text="Could not load products." />
            <Button Content="Try again" Command="{Binding RefreshCommand}" />
        </StackPanel>
    </mvux:FeedView.ErrorTemplate>

    <mvux:FeedView.ValueTemplate>
        <!-- success template -->
    </mvux:FeedView.ValueTemplate>
</mvux:FeedView>
```

**ViewModel side** (simple pattern):

```csharp
public partial record ProductsViewModel
{
    public IFeed<ImmutableArray<Product>> Products { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public ProductsViewModel(IProductsService productsService)
    {
        var feed = Feed.Async(async ct =>
        {
            var items = await productsService.GetProductsAsync(ct);
            return items.ToImmutableArray();
        });

        Products = feed;

        RefreshCommand = new AsyncRelayCommand(async () =>
        {
            await Products.RefreshAsync(); // extension in MVUX
        });
    }
}
```

*(Use the MVUX refresh helper you have in your version of `Uno.Extensions.Reactive`; naming can differ slightly by version—keep VM-side refresh close to the feed.)*

---

## How to refresh the feed from the UI

**Goal:** user taps a button → feed reloads.

```xml
<StackPanel>
    <Button Content="Refresh"
            Command="{Binding RefreshCommand}" />

    <mvux:FeedView Source="{Binding Products}">
        <mvux:FeedView.ValueTemplate>
            <ListView ItemsSource="{Binding}">
                <!-- item template -->
            </ListView>
        </mvux:FeedView.ValueTemplate>
    </mvux:FeedView>
</StackPanel>
```

**ViewModel (minimal):**

```csharp
public partial record ProductsViewModel
{
    public IFeed<IList<Product>> Products { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public ProductsViewModel(IProductsService service)
    {
        var feed = Feed.Async(async ct => await service.GetProductsAsync(ct));
        Products = feed;

        RefreshCommand = new AsyncRelayCommand(() => Products.RefreshAsync());
    }
}
```

**Point:** don’t re-create the feed; *refresh* the existing one.

---

## How to show empty feed content

**Goal:** feed succeeds but returns 0 items → show “no data”.

```xml
<mvux:FeedView Source="{Binding Products}">
    <mvux:FeedView.EmptyTemplate>
        <TextBlock Text="No products yet." HorizontalAlignment="Center" Margin="16" />
    </mvux:FeedView.EmptyTemplate>

    <mvux:FeedView.ValueTemplate>
        <ListView ItemsSource="{Binding}">
            <!-- items -->
        </ListView>
    </mvux:FeedView.ValueTemplate>
</mvux:FeedView>
```

**When it’s used:** MVUX knows the feed completed successfully but the collection was empty.

---

## How to map a service to a feed

**Goal:** turn “I have an async service” into “I have an MVUX feed”.

```csharp
public interface IProductsService
{
    Task<IEnumerable<Product>> GetProductsAsync(CancellationToken ct);
}

public partial record ProductsViewModel(IProductsService Service)
{
    public IFeed<IReadOnlyList<Product>> Products { get; } =
        Feed.Async(async ct =>
        {
            var items = await Service.GetProductsAsync(ct);
            return items.ToList();
        });
}
```

**Pattern:**

1. Define service → returns data.
2. Wrap with `Feed.Async(...)`.
3. Expose as public property.
4. Bind with `FeedView` in XAML.

---

## How to use feed with parameters

**Goal:** you want a feed that depends on user input (e.g. category).

Do it in **two properties**: the current parameter, and the feed that uses it.

```csharp
public partial record ProductsViewModel(IProductsService Service)
{
    public string Category { get; private set; } = "all";

    public IFeed<IReadOnlyList<Product>> Products { get; }

    public ProductsViewModel : this(Service)
    {
        Products = Feed.Async(async ct =>
        {
            var items = await Service.GetProductsAsync(Category, ct);
            return items.ToList();
        });
    }

    public async Task ChangeCategoryAsync(string category)
    {
        Category = category;
        await Products.RefreshAsync();
    }
}
```

**XAML trigger (example):**

```xml
<ComboBox x:Name="CategoryBox"
          SelectionChanged="CategoryBox_SelectionChanged">
    <x:String>all</x:String>
    <x:String>featured</x:String>
    <x:String>archived</x:String>
</ComboBox>
```

**Code-behind (quick):**

```csharp
private async void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (DataContext is ProductsViewModel vm &&
        CategoryBox.SelectedItem is string category)
    {
        await vm.ChangeCategoryAsync(category);
    }
}
```

**Why separate:** MVUX feed stays the same, only the input changes; you just refresh.

---

## How to show a single item feed

**Goal:** feed returns **one** object, not a collection.

```csharp
public partial record ProductDetailsViewModel(IProductsService Service)
{
    public IFeed<Product> Product { get; }

    public ProductDetailsViewModel(string productId, IProductsService service)
    {
        Product = Feed.Async(async ct =>
            await service.GetProductAsync(productId, ct));
    }
}
```

**XAML:**

```xml
<mvux:FeedView Source="{Binding Product}">
    <mvux:FeedView.ValueTemplate>
        <StackPanel Spacing="4">
            <TextBlock Text="{Binding Name}" FontSize="20" />
            <TextBlock Text="{Binding Id}" Opacity="0.6" />
        </StackPanel>
    </mvux:FeedView.ValueTemplate>
</mvux:FeedView>
```

**Note:** same component, just bind to single model instead of list.

---

## How to bind feed inside a DataTemplate

**Goal:** display feed result in a part of the page (not whole page).

```xml
<Grid>
    <TextBlock Text="Products" FontSize="18" Margin="0,0,0,8"/>

    <mvux:FeedView Source="{Binding Products}">
        <mvux:FeedView.ValueTemplate>
            <ListView ItemsSource="{Binding}">
                <ListView.ItemTemplate>
                    <DataTemplate x:DataType="local:Product">
                        <TextBlock Text="{x:Bind Name}" />
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </mvux:FeedView.ValueTemplate>
    </mvux:FeedView>
</Grid>
```

**Why:** MVUX feed is just data; you can put it inside any layout.

---

## How to test a feed viewmodel

**Goal:** unit test that the feed actually loads.

```csharp
[Fact]
public async Task ProductsFeed_Loads()
{
    var service = Substitute.For<IProductsService>();
    service.GetProductsAsync(Arg.Any<CancellationToken>())
           .Returns(Task.FromResult<IEnumerable<Product>>(new[] {
                new Product("1", "Test")
           }));

    var vm = new ProductsViewModel(service);

    var result = await vm.Products.GetAsync(); // MVUX helper

    Assert.Single(result);
    Assert.Equal("Test", result.First().Name);
}
```

**Key idea:** test the feed *source function*, not the XAML.
