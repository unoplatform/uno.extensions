# How-To: Display a Message Dialog

- Add button to MainPage 

```xml
<Button Content="Show Simple Message Dialog"
                    Click="{x:Bind ViewModel.ShowSimpleDialog}" />
```

- Add method to MainViewModel

```csharp
	public async Task ShowSimpleDialog()
    {
		_ = _navigator.ShowMessageDialogAsync(this, content: "Hello Uno Extensions!");
    }
```

- Show output (screenshot)

- Change code in MainViewModel method to await response
TODO: Remove AsResult once this PR has been merged (https://github.com/unoplatform/uno.extensions/pull/429) - do for all code samples
```csharp
	public async Task ShowSimpleDialog()
    {
		var result = await _navigator.ShowMessageDialogAsync<string>(this, content: "Hello Uno Extensions!").AsResult();
    }
```
- Show result (screenshot the debug tooltip when hover over result value in breakpoint)

- Change call to specify multiple buttons
```csharp
	public async Task ShowSimpleDialog()
    {
		var result = await _navigator.ShowMessageDialogAsync<string>(this, 
			content: "Hello Uno Extensions!",
			commands: new[]
            {
				new DialogAction("Ok"),
				new DialogAction("Cancel")
            }).AsResult();
    }
```

- Show (Screenshot) dialog with two buttons


Advanced - How to use a predefined messagedialog

Add MessageDialogViewMap to both views and routes

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
        var messageDialog = new MessageDialogViewMap(

            Content: "Hello Uno Extensions",
            Buttons: new[]
            {
                new DialogAction(Label:"Yes"),
                new DialogAction(Label:"No")
            }
        );


		views.Register(
			new ViewMap<ShellControl,ShellViewModel>(),
			new ViewMap<MainPage, MainViewModel>(),
			new ViewMap<SecondPage, SecondViewModel>(),
            messageDialog,

            localizedMessageDialog
			);

		routes
			.Register(
				new RouteMap("", View: views.FindByViewModel<ShellViewModel>() ,
						Nested: new RouteMap[]
						{
										new RouteMap("Main", View: views.FindByViewModel<MainViewModel>() ,
												IsDefault: true
												),
										new RouteMap("Second", View: views.FindByViewModel<SecondViewModel>() ,
												DependsOn:"Main"),
                                        new RouteMap("MyMessage", View: messageDialog)
                                    }));
	}
```


- In MainViewModel specify route of message dialog in showmessagedialogasync

```csharp
var result = await _navigator.ShowMessageDialogAsync<string>(this, route: "MyMessage");
```

- For localized message dialog, add 

```csharp
var localizedMessageDialog = new LocalizableMessageDialogViewMap(

    Content: localizer => localizer!["MyDialog_Content"],
	Buttons: new[]
    {
		new LocalizableDialogAction( LabelProvider:localizer=>localizer!["Dialog_Ok"]),
        new LocalizableDialogAction( LabelProvider:localizer=>localizer!["Dialog_Cancel"])
    }
);
```

- Add resources for MyDialog_Content, Dialog_Ok and Dialog_Cancel to Resources.resw

- Add Localization to host builder

```csharp
.UseLocalization()
```

- In MainViewModel change to the localized message dialgo

```csharp
        var result = await _navigator.ShowMessageDialogAsync<string>(this, route: "MyLocalizedMessage");
```

