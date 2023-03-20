---
uid: Overview.Reactive.HowTos.ListFeed
---

# How to create a list feed where values are pushed in

In this tutorial you will learn how to create an MVUX project that displays stock data
that is pushed in from a service using an
[Async Enumerable](https://learn.microsoft.com/en-us/archive/msdn-magazine/2019/november/csharp-iterating-with-async-enumerables-in-csharp-8#a-tour-through-async-enumerables) method.

1. Create an MVUX project by following the steps in
[this tutorial](xref:Overview.Reactive.HowTos.CreateMvuxProject), and name your project `StockMarketApp`.

1. Add a class named *StockMarketService.cs*, and replace its content with the following:

    ```c#
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    namespace StockMarketApp;

    public class StockMarketService
    {
        public async IAsyncEnumerable<IImmutableList<Stock>> GetCurrentMarket(
            [EnumeratorCancellation] CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                yield return _stocks.ToImmutableList();

                // this delays the next iteration by 5 seconds
                await Task.Delay(TimeSpan.FromSeconds(5), ct);

                // this updates the market prices
                UpdateMarket();
            }
        }

        private readonly Random _rnd = new();
        private void UpdateMarket()
        {
            for (int i = 0; i < _stocks.Count; i++)
            {
                var stock = _stocks[i];
                var increment = _rnd.NextDouble();

                _stocks[i] = stock with { Value = stock.Value + increment };
            }
        }

        private readonly List<Stock> _stocks = new List<Stock>
        {
            new Stock("MSFT", 279.35),
            new Stock("GOOG", 102.11),
        };
    }

    public partial record Stock(string Name, double Value);
    ```

    We're using [records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) in purpose,
    as records are immutable and ensure purity of objects. Records also implements easy equality comparison and hashing.

    The `GetCurrentMarket` emits a collection of stocks with updated values every 5 seconds.
    The 

    The `IListFeed` is a feed tailored for dealing with collections.

1. Create a file named *StockMarketModel.cs* replacing its content with the following:

    ```c#
    public partial record StockMarketModel(StockMarketService StockMarketService)
    {
        public IListFeed<Stock> Stocks => ListFeed.AsyncEnumerable(StockMarketService.GetCurrentMarket);
    }
    ```

    MVUX's analyzers will read the `StockMarketModel` and will generate a special mirrored `BindableStockMarketModel`,
    which provides binding capabilities for the View, so that we can stick to sending update message in an MVU fashion.
    
    The `Stocks` property value also gets cached, so no need to worry about its being created upon each `get`.
    
    <!-- TODO the generated code can be inspected via project->analyzers etc. -->

1. Open the file `MainView.xaml` and replace anything inside the `Page` element with the following code:

    ```xaml
    <ListView ItemsSource="{Binding Stocks}">
        <ListView.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal" Spacing="5">
                    <TextBlock Text="{Binding Name}"/>
                    <TextBlock Text="{Binding Value}"/>
                </StackPanel>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
    ```

1. Press <kbd>F7</kbd> to navigate to open code-view, and in the constructor,
after the line that calls `InitializeComponent()`, add the following line:

    ```c#
    this.DataContext = new BindableStockMarketModel(new StockMarketService());
    ```
    
    The `BindableStockMarketModel` is a special MVUX-generated model proxy class that represents
    a mirror of the `StockMarketModel` adding binding capabilities.

1. Press <kbd>F5</kbd> to run the app.

1. The app will display the initial stock values which will keep updating every 5 seconds.

    Here are 3 screenshots taken consecutively with some delay apart:

    ![](Assets/PushListFeed-1.jpg)
    ![](Assets/PushListFeed-1.jpg)
    ![](Assets/PushListFeed-1.jpg)
