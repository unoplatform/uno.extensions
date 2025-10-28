---
uid: Uno.Extensions.Mvux.FeedView
---

# FeedView Quick Guide

Compact reference for binding MVUX feeds/states to UI using `FeedView`.

## TL;DR
- Bind `FeedView.Source` to any `IFeed`, `IState`, `IListFeed`, or `IListState` exposed by the generated view model.
- Templates receive a `FeedViewState` data context (`Data`, `Refresh`, status flags) that reflects the current feed snapshot.
- Override the template slots (`ValueTemplate`, `ProgressTemplate`, `NoneTemplate`, `ErrorTemplate`, `UndefinedTemplate`) to tailor each visual state.

## Setup
```xml
<Page
    x:Class="MyMvuxApp.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">
```

## Binding a Feed
```csharp
public partial record MainModel
{
    public IFeed<Person> CurrentContact => …;
}
```
```xml
<mvux:FeedView Source="{Binding CurrentContact}">
    <DataTemplate>
        <StackPanel>
            <TextBlock Text="{Binding Data.Name}" />
            <Button Content="Refresh" Command="{Binding Refresh}" />
        </StackPanel>
    </DataTemplate>
</mvux:FeedView>
```
- `Data` resolves to the latest `Person`.
- `Refresh` triggers the feed to reload (available inside templates or via `ElementName` bindings).

## FeedViewState Essentials
- `Data`: current value from the feed; bind to properties like `Data.Name`.
- `Refresh`: async command that re-executes the underlying feed.
- `Progress`: `true` while the feed is loading or refreshing.
- `Error`: exposes the last exception, if any (useful for displaying diagnostics).
- `Parent`: original `DataContext` of the `FeedView` (use when templates need other view model members).

## Refresh Behavior
- `RefreshingState` controls whether the default progress visuals display (`Default`/`Loading`) or stay hidden (`None`).
- Default progress UI is a progress ring; customize via `ProgressTemplate` if needed.

## Template Slots
- `ValueTemplate`: default template for successful data.
- `ProgressTemplate`: shown while awaiting async work.
- `NoneTemplate`: used when the feed returns no data (`null` or empty collections).
- `ErrorTemplate`: displayed for failures; bind to `Error` for details.
- `UndefinedTemplate`: rendered before the first load occurs.

Example override:
```xml
<mvux:FeedView Source="{Binding Weather}">
    <mvux:FeedView.ValueTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Data.Summary}" />
        </DataTemplate>
    </mvux:FeedView.ValueTemplate>

    <mvux:FeedView.ProgressTemplate>
        <DataTemplate>
            <TextBlock Text="Fetching forecast…" />
        </DataTemplate>
    </mvux:FeedView.ProgressTemplate>
</mvux:FeedView>
```

## Related Topics
- [Feeds and states](xref:Uno.Extensions.Mvux.Overview)
- [State basics](xref:Uno.Extensions.Mvux.States)
- [List-state selection](xref:Uno.Extensions.Mvux.Advanced.Selection)
