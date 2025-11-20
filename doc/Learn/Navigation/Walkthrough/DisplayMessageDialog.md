---
uid: Uno.Extensions.Navigation.Walkthrough.DisplayMessageDialog
title: Display Message Dialogs
tags: [uno, uno-platform, uno-extensions, navigation, message-dialog, ShowMessageDialogAsync, MessageDialogViewMap, LocalizableMessageDialogViewMap, DialogAction, LocalizableDialogAction, confirmation-dialog, alert-dialog, user-prompt, dialog-response, button-response, dialog-buttons, localization, IStringLocalizer, predefined-dialog, reusable-dialog, route-based-dialog, ad-hoc-dialog, ContentDialog, dialog-title, dialog-content]
---

# Display Message Dialogs

## Show a simple message dialog

```csharp
public async Task ShowSimpleDialog()
{
    await _navigator.ShowMessageDialogAsync(this, title: "This is Uno", content: "Hello Uno.Extensions!");
}
```

## Capture button response

```csharp
public async Task ShowSimpleDialog()
{
    var result = await _navigator.ShowMessageDialogAsync<string>(
        this,
        title: "This is Uno",
        content: "Hello Uno.Extensions!",
        buttons:
        [
            new DialogAction("Ok"),
            new DialogAction("Cancel")
        ]);
}
```

The `result` contains the label of the selected button.

## Reuse dialog definition

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    var messageDialog = new MessageDialogViewMap(
        Title: "This is Uno",
        Content: "Hello Uno.Extensions",
        Buttons:
        [
            new DialogAction(Label:"Yes"),
            new DialogAction(Label:"No")
        ]
    );

    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<MainPage, MainViewModel>(),
        messageDialog
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new RouteMap("Main", View: views.FindByViewModel<MainViewModel>()),
                new RouteMap("MyMessage", View: messageDialog)
            ]
        )
    );
}
```

```csharp
var result = await _navigator.ShowMessageDialogAsync<string>(this, route: "MyMessage");
```

## Localize dialog content

```csharp
var localizedMessageDialog = new LocalizableMessageDialogViewMap(
    Title: localizer => localizer["MyDialog_Title"],
    Content: localizer => localizer["MyDialog_Content"],
    Buttons:
    [
        new LocalizableDialogAction(localizer => localizer["Dialog_Ok"]),
        new LocalizableDialogAction(localizer => localizer["Dialog_Cancel"])
    ]
);
```

```csharp
var result = await _navigator.ShowMessageDialogAsync<string>(this, route: "MyLocalizedMessage");
```
