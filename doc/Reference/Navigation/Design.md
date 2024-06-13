---
uid: Reference.Navigation.Design
---

# Navigation design

## INavigator

The `NavigateAsync` method on the `INavigator` interface accepts a `NavigationRequest` parameter and returns a `Task` that can be awaited in order to get a `NavigationResponse`.

```csharp
public interface INavigator
{
    Task<NavigationResponse?> NavigateAsync(NavigationRequest request);
}
```

There are `INavigator` extension methods that accept a variety of parameters, depending on the intent, which are mapped to a corresponding combination of Route and Result values.

## Navigation Controls

An application typically has one or more views responsible for controlling navigation. Eg a Frame that navigates between pages, or a TabBar that switches tabs

Navigation controls can be categorized in three distinct groups with different Navigation goals.

| Content-Based        | Has a content area that's used to display the current view                                                             |
|----------------------|------------------------------------------------------------------------------------------------------------------------|
| ContentControl       | Navigation creates an instance of a control and sets it as the Content                                                 |
| Panel (eg Grid)      | Navigation sets a child element to Visible, hiding any previously visible child. Two scenarios:<br> - An existing child is found. The child is set to Visible<br> - No child is found. A new instance of a control is created and added to the Panel. The new instance is set to visible<br>Note that when items are hidden, they're no removed from the visual tree |
| Frame                | Forward navigation adds a new page to the stack based <br>Backward navigation pops the current page off the stack<br>Combination eg forward navigation and clear back stack |
|                      |                                                                                                                        |
| **Selection-Based**      | **Has selectable items**                                                                                                 |
| NavigationView       | Navigation selects the NavigationViewitem with the correct Region.Name set                                             |
| TabBar               | Navigation selects the TabBarItem with the correct Region.Name set                                                     |
|                      |                                                                                                                        |
| **Prompt-Based (Modal)** | **Modal style prompt, typically for capturing input from user**                                                            |
| ContentDialog        | Forward navigation opens a content dialog <br>Backward navigation closes the dialog                                    |
| MessageDialog        | Forward navigation opens a MessageDialog<br>Backward navigation closes the MessageDialog                               |
| Popup                | Forward navigation opens the popup<br>Backward navigation closes the popup                                             |
| Flyout               | Forward navigation opens the flyout<br>Backward navigation closes the flyout                                           |

## Regions

A region is the abstraction of the view responsible for handling navigation.

Regions are structured into a logical hierarchical representation that shadows the navigation-aware controls in the visual hierarchy. The hierarchy allows navigation requests to be propagated up to parent and down to child regions as required.

Regions are specified by setting Region.Attached="true" on a navigation control (eg Frame, ContentControl, Grid).

```csharp
<ContentControl uen:Region.Attached="true" />
```

Pushing a view to this region:
  `navigator.NavigateRouteAsync(this,"ProductDetails");`
or
  `navigator.NavigateViewAsync<ProductDetailsControl>(this);`
or
  `navigator.NavigateViewModelAsync<ProductDetailsViewModel(this);`
or
  `navigator.NavigateDataAsync(this, selectedProduct);`
