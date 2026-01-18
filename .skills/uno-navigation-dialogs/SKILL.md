---
name: uno-navigation-dialogs
description: Display dialogs, flyouts, and modals using Uno Platform Navigation Extensions. Use when showing confirmation dialogs, picker flyouts, custom content dialogs, message dialogs, or any modal UI. Covers Qualifiers.Dialog, ContentDialog vs Page differences, and dialog result handling.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Dialog Navigation

This skill covers displaying dialogs, flyouts, and modals using Navigation Extensions.

## Dialog Types

| View Type | Display Behavior |
|-----------|------------------|
| `Page` | Displayed as a Flyout |
| `ContentDialog` | Displayed as a Modal |

## Opening Dialogs

### From Code

```csharp
// Open as dialog
await _navigator.NavigateViewAsync<SamplePage>(this, qualifier: Qualifiers.Dialog);
```

### From XAML

Use `!` prefix to indicate dialog navigation:

```xml
<Button Content="Show Options"
        uen:Navigation.Request="!Sample" />
```

## Flyout (Using Page)

Create a `Page` for flyout content:

**SamplePage.xaml:**
```xml
<Page x:Class="MyApp.Views.SamplePage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
    <Grid Padding="20">
        <StackPanel>
            <TextBlock Text="Flyout Content" FontSize="24" />
            <Button Content="Close" uen:Navigation.Request="-" />
        </StackPanel>
    </Grid>
</Page>
```

Register the route:
```csharp
new ViewMap<SamplePage, SampleViewModel>()
```

Open as flyout:
```xml
<Button Content="Show Flyout" uen:Navigation.Request="!Sample" />
```

## Modal (Using ContentDialog)

Create a `ContentDialog` for modal content:

**SampleDialog.xaml:**
```xml
<ContentDialog x:Class="MyApp.Views.SampleDialog"
               xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
               Title="Confirm Action"
               PrimaryButtonText="Ok"
               SecondaryButtonText="Cancel"
               Style="{ThemeResource DefaultContentDialogStyle}">
    <Grid Padding="20">
        <TextBlock Text="Are you sure you want to proceed?" />
    </Grid>
</ContentDialog>
```

Register and open:
```csharp
new ViewMap<SampleDialog, SampleDialogViewModel>()
```

```xml
<Button Content="Show Modal" uen:Navigation.Request="!Sample" />
```

## Passing Data to Dialogs

### XAML

```xml
<Button Content="Edit Item"
        uen:Navigation.Request="!EditDialog"
        uen:Navigation.Data="{Binding SelectedItem}" />
```

### Code

```csharp
await _navigator.NavigateViewModelAsync<EditDialogViewModel>(
    this, 
    qualifier: Qualifiers.Dialog, 
    data: selectedItem);
```

## Getting Results from Dialogs

### Register for Result

```csharp
new ResultDataViewMap<ItemPickerPage, ItemPickerViewModel, SelectedItem>()
```

### Open and Await Result

```csharp
var result = await _navigator.GetDataAsync<SelectedItem>(this);

if (result is not null)
{
    // User made a selection
    SelectedItem = result;
}
```

### Return Result from Dialog

In the dialog's ViewModel:

```csharp
public async ValueTask Confirm()
{
    await _navigator.NavigateBackWithResultAsync(this, data: _selectedItem);
}
```

Or in XAML for list selection:

```xml
<ListView ItemsSource="{Binding Items}"
          uen:Navigation.Request="-">
    <!-- Item template -->
</ListView>
```

## Message Dialogs

### Simple Message

```csharp
await _navigator.ShowMessageDialogAsync(
    this, 
    title: "Alert", 
    content: "Operation completed successfully!");
```

### With Buttons and Response

```csharp
var result = await _navigator.ShowMessageDialogAsync<string>(
    this,
    title: "Confirm",
    content: "Are you sure you want to delete?",
    buttons:
    [
        new DialogAction("Yes"),
        new DialogAction("No")
    ]);

if (result == "Yes")
{
    // User confirmed
}
```

### Predefined Message Dialog

Define reusable message dialogs in route registration:

```csharp
var confirmDialog = new MessageDialogViewMap(
    Title: "Confirm Delete",
    Content: "This action cannot be undone.",
    Buttons:
    [
        new DialogAction(Label: "Delete"),
        new DialogAction(Label: "Cancel")
    ]
);

views.Register(
    // Other views...
    confirmDialog
);

routes.Register(
    new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
        Nested:
        [
            // Other routes...
            new RouteMap("ConfirmDelete", View: confirmDialog)
        ]
    )
);
```

Use by route name:

```csharp
var result = await _navigator.ShowMessageDialogAsync<string>(this, route: "ConfirmDelete");
```

### Localized Message Dialog

```csharp
var localizedDialog = new LocalizableMessageDialogViewMap(
    Title: localizer => localizer["DeleteDialog_Title"],
    Content: localizer => localizer["DeleteDialog_Content"],
    Buttons:
    [
        new LocalizableDialogAction(localizer => localizer["Button_Delete"]),
        new LocalizableDialogAction(localizer => localizer["Button_Cancel"])
    ]
);
```

## Custom Dialog with Data

**Registration:**
```csharp
views.Register(
    new ViewMap<GenericDialog, GenericDialogModel>(Data: new DataMap<DialogInfo>())
);

routes.Register(
    new RouteMap("Dialog", View: views.FindByView<GenericDialog>())
);
```

**DialogInfo Record:**
```csharp
public record DialogInfo(string Title, string Content);
```

**Usage:**
```csharp
var dialogInfo = new DialogInfo("Warning", "This will reset all settings.");
await _navigator.NavigateDataAsync(this, dialogInfo);
```

## Closing Dialogs

### From Code

```csharp
await _navigator.NavigateBackAsync(this);
```

### From XAML

```xml
<Button Content="Close" uen:Navigation.Request="-" />
```

### ContentDialog Buttons

ContentDialog automatically closes on primary/secondary button clicks. Handle results:

```csharp
// Dialog closes automatically on button click
// Result is returned via NavigateBackWithResultAsync if needed
```

## Best Practices

1. **Use `Page` for flyouts** with custom content that may need scrolling

2. **Use `ContentDialog` for modals** with simple confirmations or forms

3. **Always handle dialog results** - users may close without selecting

4. **Use `MessageDialogViewMap`** for reusable confirmation patterns

5. **Pass data to dialogs** via `Navigation.Data` for context

6. **Return results with `NavigateBackWithResultAsync`** or list selection with `-`

7. **Use localized dialogs** for multi-language support

## Common Patterns

### Confirmation Before Delete

```xml
<Button Content="Delete"
        uen:Navigation.Request="!ConfirmDelete"
        uen:Navigation.Data="{Binding SelectedItem}" />
```

### Filter Selection

```xml
<Button Content="Filters"
        uen:Navigation.Request="!FilterDialog"
        uen:Navigation.Data="{Binding CurrentFilter, Mode=TwoWay}" />
```

### Item Picker

```csharp
var item = await _navigator.GetDataAsync<PickerItem>(this);
if (item is not null)
{
    // Use selected item
}
```
