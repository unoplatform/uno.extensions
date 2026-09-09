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
  (a `{Model}Mock.Empty` pins every input to its empty state);
- a `partial class {Vm}Mock` with `Create(...)` factories that build the **real
  view-model** with its services null-injected, then apply the mock;
- a `SetMock(this {Vm}, {Model}Mock)` extension that swaps each mocked feed.

Given this model:

```csharp
public partial record RecipeModel(IRecipeService Service)
{
    // service-dependent input
    public IListFeed<Step> Steps => ListFeed.Async(Service.GetSteps);

    // derived — recomputes over whatever Steps emits
    public IFeed<int> StepsCount => Steps.Select(steps => steps.Count);
}
```

the generator produces `RecipeModelMock`, `RecipeViewModelMock.Create(...)` and
`SetMock`. You drive the page from a test or a preview head — no scope to open,
`Create` opens it internally:

```csharp
using Uno.HotTesting.Reactive;

// Real RecipeViewModel + real RecipeModel, IRecipeService null-injected.
var vm = RecipeViewModelMock.Create(new RecipeModelMock { Steps = ListFeedMock.Value(step1, step2, step3) });

// StepsCount is NOT mocked — it recomputes through the real Select over the
// mocked Steps, so the derived feed stays truthful.

// Live transitions: keep calling SetMock to walk the states.
vm.SetMock(RecipeModelMock.Empty with { Steps = ListFeedMock.Loading<Step>() });
vm.SetMock(RecipeModelMock.Empty with { Steps = ListFeedMock.Error<Step>(new TimeoutException()) });
```

> [!IMPORTANT]
> Mocking is only active for a view-model built by `Create` (which opens a
> `MockingService.Enable()` scope around construction). Outside such a scope
> nothing is wrapped, so a shipping application pays no cost — never reference
> `Uno.HotTesting.Reactive` from a published app head.

### Only fill what matters

Required members are the **service-dependent inputs**; the compiler lists them
for you. Derived members are optional:

```csharp
// every input at once, exhaustively
vm.SetMock(new RecipeModelMock
{
    Steps = ListFeedMock.Loading<Step>(),   // required — the compiler asked for it
    // StepsCount left unset → real derivation runs over the mocked Steps
});

// or start from Empty and change one axis with `with`
vm.SetMock(RecipeModelMock.Empty with { Steps = ListFeedMock.Loading<Step>() });

// or pin a derived value directly, ignoring its inputs
vm.SetMock(RecipeModelMock.Empty with { StepsCount = FeedMock.Value(3) });
```

> [!NOTE]
> Mocking commands is not supported yet — it is planned for a future version.

### Named catalogs (one-line previews)

`{Vm}Mock` is generated as a **partial class**, so you extend it with named
catalog entries in your own file. Each entry builds the real view-model pinned to
a state through `Create`:

```csharp
// your file, same namespace as the generated RecipeViewModelMock
public static partial class RecipeViewModelMock
{
    public static RecipeViewModel Loading => Create(RecipeModelMock.Empty with { Steps = ListFeedMock.Loading<Step>() });
    public static RecipeViewModel Empty   => Create();   // every input Empty
    public static RecipeViewModel Basic   => Create(new RecipeModelMock
    {
        Steps = ListFeedMock.Value(new Step("Toast the bread"), new Step("Mash the avocado")),
    });
}
```

```xml
<!-- One-line preview binding — real page, real view-model, pinned state -->
<Page DataContext="{x:Bind mocks:RecipeViewModelMock.Basic}" />
```

### Turning the instrumentation off

The metadata the consumer generator reads is emitted **by default** (the runtime,
not the generator, decides activation). To restore byte-identical MVUX output,
opt out at the assembly level:

```csharp
[assembly: EnableFeedMocking(IsEnabled = false)]
```
