---
name: uno-navigation-message-dialog
description: Display message dialogs and confirmation prompts using Uno Platform Navigation Extensions. Use when you need to show alerts, confirmations, or simple user prompts that return a response.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Message Dialog Navigation

This skill covers displaying message dialogs using Uno Platform Navigation Extensions.

## Overview

The `ShowMessageDialogAsync` extension method provides a simple way to display message dialogs with buttons and capture user responses.

## Prerequisites

```xml
<UnoFeatures>Navigation</UnoFeatures>
```

## Using Statements

```csharp
using Uno.Extensions.Navigation;
```

## Basic Message Dialog

### Simple Alert

```csharp
await _navigator.ShowMessageDialogAsync(
    this,
    title: "Alert",
    content: "Operation completed successfully."
);
```

### With Custom Button

```csharp
await _navigator.ShowMessageDialogAsync(
    this,
    title: "Information",
    content: "Your changes have been saved.",
    buttons: [new DialogAction("OK")]
);
```

## Confirmation Dialog

### Yes/No Confirmation

```csharp
var result = await _navigator.ShowMessageDialogAsync(
    this,
    title: "Confirm Delete",
    content: "Are you sure you want to delete this item?",
    buttons:
    [
        new DialogAction("Yes"),
        new DialogAction("No")
    ]
);

if (result == "Yes")
{
    // User confirmed
    await DeleteItemAsync();
}
```

### With Cancel Option

```csharp
var result = await _navigator.ShowMessageDialogAsync(
    this,
    title: "Save Changes",
    content: "Do you want to save your changes before closing?",
    buttons:
    [
        new DialogAction("Save"),
        new DialogAction("Don't Save"),
        new DialogAction("Cancel")
    ]
);

switch (result)
{
    case "Save":
        await SaveAsync();
        await CloseAsync();
        break;
    case "Don't Save":
        await CloseAsync();
        break;
    case "Cancel":
        // Stay on current page
        break;
}
```

## DialogAction Options

### Basic DialogAction

```csharp
new DialogAction("Button Text")
```

### DialogAction with Label

```csharp
new DialogAction(label: "OK", id: "confirm")
```

The `id` is returned as the result, while `label` is displayed to the user:

```csharp
var result = await _navigator.ShowMessageDialogAsync(
    this,
    title: "Confirm",
    content: "Proceed?",
    buttons:
    [
        new DialogAction(label: "Yes, proceed", id: "yes"),
        new DialogAction(label: "No, cancel", id: "no")
    ]
);

if (result == "yes")
{
    // Proceed
}
```

## Error Dialog

```csharp
public async Task ShowErrorAsync(string message)
{
    await _navigator.ShowMessageDialogAsync(
        this,
        title: "Error",
        content: message,
        buttons: [new DialogAction("OK")]
    );
}
```

## Method Signature

```csharp
Task<string?> ShowMessageDialogAsync(
    object sender,
    string? title = null,
    string? content = null,
    IEnumerable<DialogAction>? buttons = null,
    CancellationToken cancellationToken = default
);
```

### Parameters

| Parameter | Description |
|-----------|-------------|
| `sender` | The navigation source (usually `this`) |
| `title` | Dialog title text |
| `content` | Dialog body message |
| `buttons` | Collection of `DialogAction` buttons |
| `cancellationToken` | Optional cancellation token |

### Return Value

Returns the string identifier of the clicked button, or `null` if dialog was dismissed.

## ViewModel Integration

### In ViewModel with INavigator

```csharp
public partial class OrderViewModel : ObservableObject
{
    private readonly INavigator _navigator;

    public OrderViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    [RelayCommand]
    private async Task CancelOrder()
    {
        var result = await _navigator.ShowMessageDialogAsync(
            this,
            title: "Cancel Order",
            content: "Are you sure you want to cancel this order?",
            buttons:
            [
                new DialogAction("Yes, Cancel"),
                new DialogAction("No, Keep Order")
            ]
        );

        if (result == "Yes, Cancel")
        {
            await _orderService.CancelOrderAsync(OrderId);
            await _navigator.NavigateBackAsync(this);
        }
    }
}
```

### With MVUX Model

```csharp
public partial record OrderModel(INavigator Navigator, IOrderService OrderService)
{
    public async ValueTask CancelOrder()
    {
        var result = await Navigator.ShowMessageDialogAsync(
            this,
            title: "Cancel Order",
            content: "Are you sure you want to cancel this order?",
            buttons:
            [
                new DialogAction("Yes"),
                new DialogAction("No")
            ]
        );

        if (result == "Yes")
        {
            await OrderService.CancelOrderAsync();
        }
    }
}
```

## Common Dialog Patterns

### Delete Confirmation

```csharp
public async Task<bool> ConfirmDeleteAsync(string itemName)
{
    var result = await _navigator.ShowMessageDialogAsync(
        this,
        title: "Delete Item",
        content: $"Are you sure you want to delete \"{itemName}\"? This action cannot be undone.",
        buttons:
        [
            new DialogAction(label: "Delete", id: "delete"),
            new DialogAction(label: "Cancel", id: "cancel")
        ]
    );

    return result == "delete";
}
```

### Unsaved Changes

```csharp
public async Task<bool> ConfirmDiscardChangesAsync()
{
    var result = await _navigator.ShowMessageDialogAsync(
        this,
        title: "Unsaved Changes",
        content: "You have unsaved changes. Do you want to discard them?",
        buttons:
        [
            new DialogAction(label: "Discard", id: "discard"),
            new DialogAction(label: "Keep Editing", id: "keep")
        ]
    );

    return result == "discard";
}
```

### Logout Confirmation

```csharp
public async Task LogoutAsync()
{
    var result = await _navigator.ShowMessageDialogAsync(
        this,
        title: "Logout",
        content: "Are you sure you want to logout?",
        buttons:
        [
            new DialogAction("Logout"),
            new DialogAction("Cancel")
        ]
    );

    if (result == "Logout")
    {
        await _authService.LogoutAsync();
        await _navigator.NavigateRouteAsync(this, "/Login", qualifier: Qualifiers.ClearBackStack);
    }
}
```

### Network Error

```csharp
public async Task<bool> HandleNetworkErrorAsync()
{
    var result = await _navigator.ShowMessageDialogAsync(
        this,
        title: "Connection Error",
        content: "Unable to connect to the server. Would you like to retry?",
        buttons:
        [
            new DialogAction("Retry"),
            new DialogAction("Cancel")
        ]
    );

    return result == "Retry";
}
```

## Handling Null Result

The dialog can return `null` if dismissed without clicking a button:

```csharp
var result = await _navigator.ShowMessageDialogAsync(
    this,
    title: "Confirm",
    content: "Proceed?",
    buttons:
    [
        new DialogAction("Yes"),
        new DialogAction("No")
    ]
);

if (result is null)
{
    // Dialog was dismissed (e.g., pressing Escape or clicking outside)
    return;
}

if (result == "Yes")
{
    // User clicked Yes
}
```

## Best Practices

1. **Keep messages concise** - Users rarely read long dialog text

2. **Use clear button labels** - "Delete" is better than "OK" for delete confirmations

3. **Put primary action first** - Most important action should be first button

4. **Handle null results** - User may dismiss without clicking a button

5. **Use for important decisions** - Don't overuse dialogs for minor confirmations

6. **Consider accessibility** - Ensure dialog title clearly conveys purpose

## Common Issues

| Issue | Solution |
|-------|----------|
| Dialog not showing | Ensure navigation is properly configured |
| Result always null | Check that buttons are properly defined |
| Dialog dismissed unexpectedly | Handle null result case |
| Button text not showing | Ensure DialogAction label is set |
