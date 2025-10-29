---
uid: Uno.Extensions.Mvux.HowToPushListFeed
---

# MVUX Push List Feed

Stream collections into MVUX views by connecting `IAsyncEnumerable` sources to `IListFeed<T>` and binding them with `FeedView`.

## Stream stock updates from a service

```csharp
using System.Runtime.CompilerServices;

namespace StockMarketApp;

public partial record Stock(string Name, double Value);

public class StockMarketService
{
    readonly List<Stock> _stocks =
    [
        new("MSFT", 279.35),
        new("GOOG", 102.11)
    ];

    public async IAsyncEnumerable<IImmutableList<Stock>> GetCurrentMarket(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var random = new Random();

        while (!ct.IsCancellationRequested)
        {
            yield return _stocks.ToImmutableList();

            await Task.Delay(TimeSpan.FromSeconds(5), ct);

            for (var i = 0; i < _stocks.Count; i++)
            {
                var stock = _stocks[i];
                _stocks[i] = stock with { Value = stock.Value + random.NextDouble() };
            }
        }
    }
}
```

Each iteration sends a fresh immutable snapshot, simulating server-side price changes.

## Expose the stream through the model

```csharp
public partial record StockMarketModel(StockMarketService StockMarketService)
{
    public IListFeed<Stock> Stocks =>
        ListFeed.AsyncEnumerable(StockMarketService.GetCurrentMarket);
}
```

`ListFeed.AsyncEnumerable` bridges the async enumerable into an MVUX-friendly feed.

## Display live updates in the UI

```xml
<Page ...
      xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    <mvux:FeedView Source="{Binding Stocks}">
        <DataTemplate>
            <ListView ItemsSource="{Binding Data}" SelectionMode="None">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal" Spacing="8">
                            <TextBlock Text="{Binding Name}" />
                            <TextBlock Text="{Binding Value}" />
                        </StackPanel>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </DataTemplate>
    </mvux:FeedView>
</Page>
```

```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        DataContext = new StockMarketViewModel(new StockMarketService());
    }
}
```

`FeedView` refreshes automatically whenever the service pushes another snapshot.

## Resources

- List feed fundamentals: (xref:Uno.Extensions.Mvux.HowToListFeed)
- List state guidance: (xref:Uno.Extensions.Mvux.ListStates)
- FeedView customization: (xref:Uno.Extensions.Mvux.FeedView)
- Sample app: https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/StockMarketApp
