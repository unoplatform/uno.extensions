---
uid: Uno.Extensions.Navigation.Walkthrough.ShowDialog
title: Display Dialogs as Flyouts or Modals
tags: [uno, uno-platform, uno-extensions, navigation, dialog, modal, flyout, Qualifiers.Dialog, ContentDialog, Navigation.Request, Page, overlay-navigation, dialog-qualifier, dialog-navigation, exclamation-prefix, view-type, modal-dialog, flyout-dialog, PrimaryButtonText, SecondaryButtonText, DefaultContentDialogStyle, ViewMap, RouteMap, NavigateViewAsync]
---

# Display Dialogs as Flyouts or Modals

> **UnoFeature:** Navigation

## Show dialog from code

```csharp
private void ShowDialogClick(object sender, RoutedEventArgs e)
{
    _ = this.Navigator()?.NavigateViewAsync<SamplePage>(this, qualifier: Qualifiers.Dialog);
}
```

## Show dialog from XAML

```xml
<Button Content="Show flyout from XAML"
        uen:Navigation.Request="!Sample" />
```

The `!` prefix indicates dialog navigation.

## Display as flyout

```xml
<Page x:Class="ShowDialog.Views.SamplePage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
    <Grid>
        <TextBlock Text="Dialog Navigation"
                   FontSize="32"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
    </Grid>
</Page>
```

Using `Page` displays content as a flyout.

## Display as modal

```xml
<ContentDialog x:Class="ShowDialog.Views.SamplePage"
               xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
               Title="Modal"
               PrimaryButtonText="Ok"
               SecondaryButtonText="Cancel"
               Style="{ThemeResource DefaultContentDialogStyle}">
    <Grid>
        <TextBlock HorizontalAlignment="Center"
                   VerticalAlignment="Center"
                   FontSize="32"
                   Text="Dialog Navigation" />
    </Grid>
</ContentDialog>
```

Using `ContentDialog` displays content as a modal.
