---
uid: Uno.Extensions.Navigation.Walkthrough.DisplayItemDetails
title: Pass Data During Navigation
tags: [uno, uno-platform, uno-extensions, navigation, data-passing, DataViewMap, NavigateDataAsync, ListView, master-detail, type-based-navigation, constructor-injection, data-injection, item-details, data-parameter, NavigateViewModelAsync, route-by-type, polymorphic-routing, derived-types, record-types, ListView-selection, auto-data-passing, detail-view, master-view]
---

# Pass Data During Navigation

> **UnoFeature:** Navigation

## Pass data to another page

```csharp
public record Widget(string Name, double Weight);
```

```csharp
new DataViewMap<SecondPage, SecondViewModel, Widget>()
```

```csharp
public async Task GoToSecondPage()
{
    var widget = new Widget("CrazySpinner", 34.0);
    await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data: widget);
}
```

```csharp
public class SecondViewModel
{
    public string Name { get; }

    public SecondViewModel(Widget widget)
    {
        Name = widget.Name;
    }
}
```

Data is injected via constructor dependency injection.

## Navigate by data type

```csharp
await _navigator.NavigateDataAsync(this, data: widget);
```

Navigation resolves the route based on the data type using DataViewMap registration.

## Pass selected list item

```csharp
public Widget[] Widgets { get; } =
[
    new Widget("NormalSpinner", 5.0),
    new Widget("HeavySpinner", 50.0)
];
```

```xml
<ListView ItemsSource="{Binding Widgets}"
          uen:Navigation.Request=""
          HorizontalAlignment="Center"
          VerticalAlignment="Center">
    <ListView.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Padding="10">
                <TextBlock Text="{Binding Name}" />
                <TextBlock Text="{Binding Weight}" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

Empty `Navigation.Request=""` enables automatic data-based routing from list selection.

## Route different types to different pages

```csharp
public record Entity(string Name);
public record SecondEntity(string Name) : Entity(Name);
public record ThirdEntity(string Name) : Entity(Name);
```

```csharp
new DataViewMap<SecondPage, SecondViewModel, SecondEntity>(),
new DataViewMap<ThirdPage, ThirdViewModel, ThirdEntity>()
```

```csharp
public Entity[] Items { get; } =
[
    new SecondEntity("Second Entity"),
    new ThirdEntity("Third Entity")
];
```

Navigation automatically routes to `SecondPage` or `ThirdPage` based on the selected item's type.
