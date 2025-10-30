# 1. Show a list that updates from a pushed source using MVUX

**Goal:** UI shows a list of stocks that keeps changing because the data is *pushed* (not polled by the UI).

**Dependencies:**

* `Uno.Extensions`
* `Uno.Extensions.Reactive` (for `IFeed<T>`, `IListFeed<T>`, `ListFeed`)

**Model**

```csharp
using Uno.Extensions.Mvux;
using System.Collections.Immutable;

public partial record Stock(string Name, double Value);

public partial record StockMarketModel(StockMarketService Service)
{
    // IListFeed<T> = “a list that can be pushed to the UI”
    public IListFeed<Stock> Stocks =>
        ListFeed.AsyncEnumerable(Service.GetCurrentMarket);
}
```

**View (XAML)**

```xml
<ListView ItemsSource="{Binding Stocks}" SelectionMode="None">
    <ListView.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBlock Text="{Binding Name}" />
                <TextBlock Text="{Binding Value}" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

**Code-behind (wire ViewModel)**

```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        DataContext = new StockMarketViewModel(new StockMarketService());
    }
}
```

**What happens**

* `StockMarketModel` is turned by MVUX analyzers into `StockMarketViewModel`.
* `Stocks` is a *list feed*, so every time the service yields a new list, the UI gets a new list.
* No manual refresh button needed.

---

## 2. Push data with an `IAsyncEnumerable<T>` every few seconds

**Goal:** produce data in *push* mode using `IAsyncEnumerable<T>`; the UI will receive each yielded list.

**Service**

```csharp
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

public class StockMarketService
{
    private readonly List<Stock> _stocks =
    [
        new Stock("MSFT", 279.35),
        new Stock("GOOG", 102.11),
    ];

    public async IAsyncEnumerable<IImmutableList<Stock>> GetCurrentMarket(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var rnd = new Random();

        while (!ct.IsCancellationRequested)
        {
            // 1. push current prices
            yield return _stocks.ToImmutableList();

            // 2. wait before pushing again
            await Task.Delay(TimeSpan.FromSeconds(5), ct);

            // 3. simulate server-side price changes
            for (int i = 0; i < _stocks.Count; i++)
            {
                var stock = _stocks[i];
                var increment = rnd.NextDouble();
                _stocks[i] = stock with { Value = stock.Value + increment };
            }
        }
    }
}
```

**Key points**

* The method returns `IAsyncEnumerable<IImmutableList<Stock>>`.
* Each `yield return` = one push.
* `ListFeed.AsyncEnumerable(service.GetCurrentMarket)` turns that into an `IListFeed<Stock>`.
* Immutable list is important so the UI gets a clean snapshot.

---

## 3. Expose an async enumerable as a list feed

**Goal:** turn a method like `Task<IAsyncEnumerable<...>>` or `IAsyncEnumerable<...>` into something the view can bind to.

**Model**

```csharp
using Uno.Extensions.Reactive;
using System.Collections.Immutable;

public partial record StockMarketModel(StockMarketService Service)
{
    // Service returns IAsyncEnumerable<IImmutableList<Stock>>
    public IListFeed<Stock> Stocks =>
        ListFeed.AsyncEnumerable(Service.GetCurrentMarket);
}
```

**Why this works**

* MVUX understands `ListFeed.AsyncEnumerable(...)`.
* It generates a bindable property on the ViewModel.
* You don’t manually subscribe to the async stream.

---

## 4. Bind a list feed to a simple list view

**Goal:** have XAML react to each new list from the feed.

**XAML**

```xml
<ListView ItemsSource="{Binding Stocks}" SelectionMode="None">
    <ListView.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Spacing="5">
                <TextBlock Text="{Binding Name}" />
                <TextBlock Text="{Binding Value}" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

**Notes**

* You bind to the *name* of the property in the model: `Stocks`.
* MVUX created the ViewModel (`StockMarketViewModel`) for you.
* No `INotifyPropertyChanged` boilerplate needed.

---

## 5. Show feed status automatically (with `FeedView` variant)

**Goal:** show loading / error / empty states without writing UI state logic.

**XAML**

```xml
<mvx:FeedView Source="{Binding Stocks}">
    <mvx:FeedView.LoadingTemplate>
        <ProgressRing IsActive="True" />
    </mvx:FeedView.LoadingTemplate>

    <mvx:FeedView.NoneTemplate>
        <TextBlock Text="No stocks yet." />
    </mvx:FeedView.NoneTemplate>

    <mvx:FeedView.ErrorTemplate>
        <TextBlock Text="Could not load stocks." />
    </mvx:FeedView.ErrorTemplate>

    <mvx:FeedView.ValueTemplate>
        <ListView ItemsSource="{Binding}">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" Spacing="5">
                        <TextBlock Text="{Binding Name}" />
                        <TextBlock Text="{Binding Value}" />
                    </StackPanel>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </mvx:FeedView.ValueTemplate>
</mvx:FeedView>
```

**What this does**

* `Source="{Binding Stocks}"` = bind to feed.
* Each visual state is declared separately.
* You don’t write `if (IsLoading) ...` code-behind.