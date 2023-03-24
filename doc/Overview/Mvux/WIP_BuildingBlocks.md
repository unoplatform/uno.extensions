---
uid: Overview.Mvux.BuildingBlocks
---

## MVUX building blocks

The most important components in an MVUX app is either a feed (`IFeed<T>`), which is used for read-only scenarios,
or a state (`IState<T>`) which should be used when the user can apply changes to the data to be sent back to the model.

There is also a list flavor of the above components (`IListFeed<T>`/`IListState<T>` respectively).
The list version of feed and state offer additional operators for working with collections.

Here's another example of a model exposing data coming from a service.
This time we're using an `IListFeed`, as the data is a collection of entities.

```c#
public IListFeed<Product> Products => ListFeed.Async(_productService.GetProducts);
```

For the view side, a special `FeedView` control is used, it's especially designed to interact with the feed and its metadata.

One of the things MVUX code-generation engine takes care of,
is caching the `Products` property value.  
You don't need to worry about the service getting invoked upon each get.

```xaml
<Page
    x:Class="MyProject.MyPage"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:mvux="using:Uno.Extensions.Reactive.UI">

    <mvux:FeedView Source="{Binding Products}">
        <DataTemplate>
            <ListView ItemsSource="{Binding Data}" />
        </DataTemplate>
    <mvux:FeedView.LoadingTemplate>

</mvux:FeedView>
```

MVUX also generates code which serves the `FeedView` with helper methods and commands to enable easy refreshing of data,
as well as propagating data-update messages back to the model.

> [!TIP]
> The `FeedView` provides support for additional feed states, such as when the service returned no records, failed, and more.
> Checkout its `Template`-suffixed properties.

<!-- TODO once ready link in references -->
