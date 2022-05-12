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
		_ = _navigator.ShowMessageDialogAsync<string>(this, content: "Hello Uno Extensions!");
    }
```

- Show output (screenshot)

WARNING: Currently there's a bug where you need to add .UseLocalization() to the app.xaml.host.cs - PR to fix:https://github.com/unoplatform/uno.extensions/pull/426

- Change code in MainViewModel method to await response

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




- show how to display simple message dialog using ShowMessageDialogAsync
- show how to retrieve result from message dialog
- show how to display a predefined message dialog using MessageDialogViewMap (show how to override properties as necessary)
- show how to display a localize message dialog