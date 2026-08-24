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
`DataContext` injection. These factories are the reusable runtime base for the
longer-term Hot Testing direction; this initial API does not promise or require a
generator.
