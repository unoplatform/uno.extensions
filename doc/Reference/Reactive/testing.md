---
uid: Uno.Extensions.Reactive.Testing
---
# Test feed

In order to test your reactive application, you should install the `Uno.Extensions.Reactive.Testing` package in your test project.

Make your test class inherit from `FeedTests`, then in your tests methods, you can use the `.Record()` extensions method on the test you want to test.
It will subscribe to your feed and persist all received messages. Then you can assert the expected messages using the fluent assertions:

```csharp
[TestMethod]
public async Task When_ProviderReturnsValueSync_Then_GetSome()
{
    var sut = Feed.Async(async ct =>
    {
        await Task.Delay(500, ct);
        return 42;
    });
    using var result = await sut.Record();

    result.Should().Be(r => r
        .Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
        .Message(Changed.Data, 42, Error.No, Progress.Final)
    );
}
```

You define each axis (`Data` / `Error` / `Progress`) in the `Message` you want to validate. You can also define which axes are expected to have changed (`Changed`).

> [!NOTE]
> When developing a new _feed_, we recommend that you systematically validate all axes.

## Provide handwritten feed mocks to a page

Install the `Uno.HotTesting.Reactive` package in the project that provides the
mock page. Its `FeedMock` and `ListFeedMock` factories create feeds pinned to
common MVUX states for previews and UI testing.

In this initial scope, no view-model mock is generated. Write the record or class
yourself, give its properties exactly the names and feed types expected by the
page bindings, and assign an instance to the page's `DataContext`.

For example, these bindings expect properties named `MyFeed` and `MyItems`:

```xml
<Page
    x:Class="MyApp.MySuperPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    <StackPanel>
        <mvux:FeedView Source="{Binding MyFeed}">
            <DataTemplate>
                <TextBlock Text="{Binding Data}" />
            </DataTemplate>
        </mvux:FeedView>

        <mvux:FeedView Source="{Binding MyItems}">
            <DataTemplate>
                <ListView ItemsSource="{Binding Data}">
                    <ListView.ItemTemplate>
                        <DataTemplate>
                            <TextBlock Text="{Binding Name}" />
                        </DataTemplate>
                    </ListView.ItemTemplate>
                </ListView>
            </DataTemplate>
        </mvux:FeedView>
    </StackPanel>
</Page>
```

The handwritten mock and page initialization can then be:

```csharp
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Reactive;
using Uno.HotTesting.Reactive;

namespace MyApp;

public sealed record Product(string Name);

public sealed record MySuperPageViewModelMock
{
    public IFeed<string> MyFeed { get; init; } = FeedMock.Undefined<string>();

    public IListFeed<Product> MyItems { get; init; } = ListFeedMock.Empty<Product>();
}

public sealed partial class MySuperPage : Page
{
    public MySuperPage()
    {
        InitializeComponent();

        DataContext = new MySuperPageViewModelMock
        {
            MyFeed = FeedMock.Value("Ready"),
            MyItems = ListFeedMock.Value(
                new Product("First item"),
                new Product("Second item"))
        };
    }
}
```

The developer remains responsible for the property names, feed shapes, and
`DataContext` injection. This handwritten approach needs no generator and is handy
for pages without a view-model; for a real generated view-model driven through
mocked states, use the generated mocks described below.

## Generated view-model mocks (Hot Testing)

The handwritten record above is enough for a page that has no view-model, but as
soon as you want to drive a **real generated view-model** — its real `Model`, its
real business logic — through mocked states, let the `Uno.HotTesting.Reactive`
generator build the plumbing for you.

Reference the `Uno.HotTesting.Reactive` package in the **test or preview project**
(the one that references the app). For every MVUX model it finds, the generator
emits, next to the model:

- a `record {Model}Mock` whose **required** members are exactly the
  service-dependent feeds, and whose **optional** members are the derived feeds
  and the commands;
- `{Vm}.Create(...)` factories that build the **real view-model** with its
  services null-injected, then apply the mock;
- a `SetModel(this {Vm}, {Model}Mock)` extension that swaps each mocked feed.

Given this model:

```csharp
public partial record RecipeModel(IRecipeService Service)
{
    // service-dependent input
    public IListFeed<Step> Steps => ListFeed.Async(Service.GetSteps);

    // derived — recomputes over whatever Steps emits
    public IFeed<int> StepsCount => Steps.Select(steps => steps.Count);

    // a command
    public async ValueTask Save(CancellationToken ct) => await Service.Save(ct);
}
```

the generator produces `RecipeModelMock`, `RecipeViewModelMock.Create(...)` and
`SetModel`. You drive the page from a test or a preview head:

```csharp
using Uno.HotTesting.Reactive;

using (MockingService.Enable())
{
    // Real RecipeViewModel + real RecipeModel, IRecipeService null-injected.
    var vm = RecipeViewModelMock.Create(ListFeedMock.Value(step1, step2, step3));

    // StepsCount is NOT mocked — it recomputes through the real Select over the
    // mocked Steps, so the derived feed stays truthful.

    // Live transitions: keep calling SetModel to walk the states.
    vm.SetModel(new RecipeModelMock { Steps = ListFeedMock.Loading<Step>() });
    vm.SetModel(new RecipeModelMock { Steps = ListFeedMock.Error<Step>(new TimeoutException()) });
}
```

> [!IMPORTANT]
> Mocking is only active inside a `MockingService.Enable()` scope. Outside a
> scope nothing is wrapped, so a shipping application pays no cost — never
> reference `Uno.HotTesting.Reactive` from a published app head.

### Only fill what matters

Required members are the **service-dependent inputs**; the compiler lists them
for you. Derived members and commands are optional:

```csharp
// every input at once, exhaustively
vm.SetModel(new RecipeModelMock
{
    Steps = ListFeedMock.Loading<Step>(),   // required — the compiler asked for it
    // StepsCount left unset → real derivation runs over the mocked Steps
    // Save left unset → idle no-op command
});

// or pin a derived value directly, ignoring its inputs
vm.SetModel(RecipeModelMock.Empty with { StepsCount = FeedMock.Value(3) });
```

### Mocking commands

Commands have no feed to swap, so override them with the `CommandMock` vocabulary:

```csharp
var executed = false;
var vm = RecipeViewModelMock.Create(new RecipeModelMock
{
    Steps = ListFeedMock.Value(step1),
    Save  = CommandMock.Callback(_ => executed = true),
});

vm.Save.Execute(null);   // invokes the callback
```

`CommandMock` offers `Idle()`, `Disabled()`, `Executing()` and
`Callback(onExecute)`.

### One-liners and named catalogs

Because `Create` takes only the required inputs, the common cases are one call:

```csharp
var loading = RecipeViewModelMock.Create(ListFeedMock.Loading<Step>());
var empty   = RecipeViewModelMock.Create();                       // every input Empty
var ready   = RecipeViewModelMock.Create(ListFeedMock.Value(step1, step2));
```

Collect them into a hand-written catalog in your preview project, then bind a
page to one entry — real page, real view-model, pinned state:

```csharp
public static class RecipeCatalog
{
    public static RecipeViewModel Loading => RecipeViewModelMock.Create(ListFeedMock.Loading<Step>());
    public static RecipeViewModel Empty   => RecipeViewModelMock.Create();
    public static RecipeViewModel Basic   => RecipeViewModelMock.Create(ListFeedMock.Value(
        new Step("Toast the bread"),
        new Step("Mash the avocado")));
}
```

```xml
<!-- One-line preview binding -->
<Page DataContext="{x:Bind catalog:RecipeCatalog.Basic}" />
```

### Turning the instrumentation off

The metadata the consumer generator reads is emitted **by default** (the runtime,
not the generator, decides activation). To restore byte-identical MVUX output,
opt out at the assembly level:

```csharp
[assembly: EnableFeedMocking(IsEnabled = false)]
```
