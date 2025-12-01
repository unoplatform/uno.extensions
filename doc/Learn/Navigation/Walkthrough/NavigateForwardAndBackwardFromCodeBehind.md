---
uid: Uno.Extensions.Navigation.Walkthrough.NavigateCodeBehind
title: Navigate Forward and Backward from Code-Behind
tags: [uno, uno-platform, uno-extensions, navigation, INavigator, code-behind, xaml-cs, NavigateViewAsync, NavigateBackAsync, Navigator-extension-method, page-navigation, view-navigation, forward-navigation, backward-navigation, ViewMap, RouteMap, click-handler, event-handler, imperative-navigation]
---

# Navigate between two pages from Code-Behind

> **UnoFeature:** Navigation

## Navigate forward to another page via button click

```xml
<Button Content="Go to SamplePage"
        Click="GoToSamplePageClick" />
```

```csharp
private void GoToSamplePageClick(object sender, RoutedEventArgs e)
{
    _ = this.Navigator()?.NavigateViewAsync<SamplePage>(this);
}
```

`Navigator()` extension method provides access to navigation from any Page.

## Navigate back to previous page via button click

```xml
<Button Content="Go Back"
        Click="GoBackClick" />
```

```csharp
private void GoBackClick(object sender, RoutedEventArgs e)
{
    _ = this.Navigator()?.NavigateBackAsync(this);
}
```
