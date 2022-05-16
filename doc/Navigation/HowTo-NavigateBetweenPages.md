# How-To: Navigate Between Pages

This topic covers using Navigation to navigate between two pages using frame-based navigation. 

> [!Tip] This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions` template to create the solution. Instructions for creating an application from the template can be found [here](../Extensions/GettingStarted/UsingUnoExtensions.md)

## Step-by-steps

- Add new page, SamplePage.xaml
- Add a button "Go to Sample Page" and in the event handler  

**C#**  
```csharp
    private void GoToSamplePageClick(object sender, RoutedEventArgs e)
    {
		_ = this.Navigator()?.NavigateViewAsync<SamplePage>(this);
    }
```

- On SamplePage add a button "Go back" and in event handler

**C#**  
```csharp
    private void GoBackClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateBackAsync(this);
    }
```

- On MainPage add another button and in event handler

**C#**  
```csharp
	private void GoToSamplePageClearStackClick(object sender, RoutedEventArgs e)
    {
		_ = this.Navigator()?.NavigateViewAsync<SamplePage>(this, qualifier:Qualifiers.ClearBackStack);
    }
```
- After navigating to sample page, go back button doesn't work, since the frame backstack is empty


- Note that in Output window (add screenshot?) there is a line that says
`Uno.Extensions.Navigation.RouteResolverDefault: Information: DefaultMapping - For better performance (avoid reflection), create mapping for for path 'Sample', view 'SamplePage', view model ''`

- Add ViewMap and RouteMap

```csharp
	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<ShellControl,ShellViewModel>(),
			new ViewMap<MainPage, MainViewModel>(),
			new ViewMap<SecondPage, SecondViewModel>(),
			new ViewMap<SamplePage>()
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
                                        new RouteMap("Sample", View: views.FindByView<SamplePage>()),
                        }));
	}
```

- We can also wire up a viewmodel to our page - create a new class SampleViewModel in the ViewModels folder of the non-UI project

```csharp
public class SampleViewModel
{
	public string Title => "Sample Page";
    public SampleViewModel()
    {
    }
}
```

- Add TextBlock to the Sample page and databind to Title property
```xml
<TextBlock Text="{Binding Title}" />
```

- Update ViewMap to include SampleViewModel
```csharp
new ViewMap<SamplePage, SampleViewModel>()
```

- Show (screenshot) data bound textblock

- Can move navigation to SampleViewModel by taking a dependency on INavigator
```csharp
    private readonly INavigator _navigator;
    public SampleViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }
    public Task GoBack()
    {
        return _navigator.NavigateBackAsync(this);
    }
```

- Need to expose ViewModel property to allow x:Bind

```csharp
    public SampleViewModel? ViewModel { get; private set; }

    public SamplePage()
    {
        this.InitializeComponent();

        DataContextChanged += (_, changeArgs) => ViewModel = changeArgs.NewValue as SampleViewModel;
    }
```

- Add button to XAML and x:Bind Click to GoBack method
```xml
<Button Content="Go Back (View Model)"
                    Click="{x:Bind ViewModel.GoBack}" />
```



>> Go back and forward to different page eg ./NewPage
>> Go to multiple pages eg NextPage/AnotherPage










