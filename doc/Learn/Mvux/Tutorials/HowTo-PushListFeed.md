---
uid: Uno.Extensions.Mvux.HowToPushListFeed
---

# MVUX Push List Feed

Quick walkthrough for streaming data into an MVUX view using `IListFeed<T>` backed by an `IAsyncEnumerable<T>`.

## TL;DR
- Implement a service method that yields `IImmutableList<T>` snapshots via `IAsyncEnumerable<T>`.
- Publish the stream from your model with `ListFeed.AsyncEnumerable`.
- Bind `FeedView.Source` (or controls that understand list feeds) to display live updates.

## 1. Service Streaming Snapshots
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
            yield return _stocks.ToImmutableList();   // emit current snapshot

            await Task.Delay(TimeSpan.FromSeconds(5), ct); // wait before pushing next update

            for (var i = 0; i < _stocks.Count; i++)
            {
                var stock = _stocks[i];
                _stocks[i] = stock with { Value = stock.Value + random.NextDouble() };
            }
        }
    }
}
```
- Using records keeps stock entries immutable.
- Service simulates server-side updates by mutating internal list before emitting a new snapshot.

## 2. Surface as an IListFeed
```csharp
public partial record StockMarketModel(StockMarketService StockMarketService)
{
    public IListFeed<Stock> Stocks =>
        ListFeed.AsyncEnumerable(StockMarketService.GetCurrentMarket);
}
```
- MVUX generates `StockMarketViewModel` with the bindable `Stocks` feed.
- Consumers can `await Stocks` to capture the latest list if needed.

## 3. Bind the View
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
- `FeedView` keeps the list refreshed every time the async enumerable yields a new snapshot.
- Customize progress/error/none templates if desired; see (xref:Uno.Extensions.Mvux.FeedView).

## Related Material
- `IListFeed` overview: (xref:Uno.Extensions.Mvux.HowToListFeed)
- State-based collections: (xref:Uno.Extensions.Mvux.ListStates)
- Sample project: https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/StockMarketApp
