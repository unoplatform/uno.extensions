---
uid: Uno.Extensions.Navigation.Walkthrough.SelectValue
title: Request and Return Data from Navigation
tags: [uno, uno-platform, uno-extensions, navigation, GetDataAsync, ResultDataViewMap, data-return, value-selection, navigation-result, result-data, user-selection, data-request, return-value, await-result, ListView-selection, Navigation.Request, back-navigation, type-resolution, generic-navigation, selection-page, picker-page]
---

# Request and Return Data from Navigation

> **UnoFeatures:** `Navigation` (add to `<UnoFeatures>` in your `.csproj`)

## Request data from another page

```csharp
public record Widget(string Name, double Weight);
```

```xml
<ListView ItemsSource="{Binding Widgets}"
          uen:Navigation.Request="-"
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

```csharp
public async Task GoToSecondPage()
{
    var widget = await _navigator.GetDataAsync<SecondViewModel, Widget>(this);
}
```

`Navigation.Request="-"` navigates back and returns the selected item.

## Simplify with result type registration

```csharp
new ResultDataViewMap<SecondPage, SecondViewModel, Widget>()
```

```csharp
public async Task GoToSecondPage()
{
    var widget = await _navigator.GetDataAsync<Widget>(this);
}
```

Navigation automatically resolves which view to navigate to based on the result type.
