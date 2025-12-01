---
uid: Uno.Extensions.Navigation.Walkthrough.Advanced.AdvancedPageNavigation
title: Manage Back Stack and Multi-Page Navigation
tags: [uno, uno-platform, uno-extensions, navigation, back-stack, Qualifiers.ClearBackStack, Qualifiers.NavigateBack, NavigateRouteAsync, multi-page-navigation, navigation-qualifiers, back-stack-management, route-navigation, Route.Combine, route-qualifier, clear-stack, remove-pages, navigation-path, complex-navigation, Navigation.Request, slash-prefix, route-string, navigation-hierarchy]
---

# Manage Back Stack and Multi-Page Navigation

> **UnoFeatures:** `Navigation` (add to `<UnoFeatures>` in your `.csproj`)

## Clear back stack when navigating

```xml
<Button Content="Go to Second Page Clear Stack"
        uen:Navigation.Request="-/Second" />
```

```csharp
public async Task GoToSecondPageClearBackStack()
{
    await _navigator.NavigateViewModelAsync<SecondViewModel>(this, qualifier: Qualifiers.ClearBackStack);
}
```

Use `-/` qualifier prefix to clear back stack.

## Remove current page when navigating forward

```xml
<Button Content="Go to Sample Page"
        uen:Navigation.Request="-Sample" />
```

```csharp
public async Task GoToSamplePage()
{
    await _navigator.NavigateViewModelAsync<SampleViewModel>(this, qualifier: Qualifiers.NavigateBack);
}
```

Navigates forward and removes current page from back stack.

## Navigate through multiple pages

```csharp
await _navigator.NavigateRouteAsync(this, "Second/Sample");
```

```xml
<Button Content="Navigate to Second then Sample"
        uen:Navigation.Request="Second/Sample" />
```

Navigates to `SamplePage` with `SecondPage` in the back stack.

```csharp
public SecondViewModel? ViewModel => DataContext as SecondViewModel;
```

* Implement method in `SecondViewModel`:

```csharp
    public async Task GoToSamplePage()
    {
        await _navigator.NavigateViewModelAsync<SampleViewModel>(this, qualifier: Qualifiers.NavigateBack);
    }
```

* Result: SecondPage removed from back stack after navigating to SamplePage.

## Navigate to Multiple Pages

Navigate forward and inject additional page into back stack.

* Add button in `MainPage.xaml`:

    ```xml
    <Button Content="Go to Sample Page"
            Click="{x:Bind ViewModel.GoToSamplePage}" />
    ```

* Use multi-section route:

    ```csharp
    public async Task GoToSamplePage()
    {
        await _navigator.NavigateRouteAsync(this, route: "Second/Sample");
    }
    ```

* Route `"Second/Sample"`:
  * Navigates to SamplePage
  * Injects SecondPage into back stack
  * SecondPage created only when user navigates back
