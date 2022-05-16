# How-To: Navigate in Code

- Navigation works both in code behind and in viewmodels with same abstraction

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

- We can also wire up a viewmodel to our page - create a new class SampleViewModel in the ViewModels folder of the non-UI project

```csharp
public class SampleViewModel
{
	public string Title => "Sample Page";
    private readonly INavigator _navigator;
    public SampleViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }
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

- Add TextBlock to the Sample page and databind to Title property
```xml
<TextBlock Text="{Binding Title}" />
```

- Add ViewMap and RouteMap

```csharp
	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<ShellControl,ShellViewModel>(),
			new ViewMap<MainPage, MainViewModel>(),
			new ViewMap<SecondPage, SecondViewModel>(),
			new ViewMap<SamplePage, SampleViewModel>()
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
                                        new RouteMap("Sample", View: views.FindByViewModel<SampleViewModel>()),
                        }));
	}
```

- Update navigation method in MainPage

**C#**  
```csharp
    private void GoToSamplePageClick(object sender, RoutedEventArgs e)
    {
		_ = this.Navigator()?.NavigateViewModelAsync<SampleViewModel>(this);
    }
```


- Can move navigation to SampleViewModel by taking a dependency on INavigator
```csharp

    public Task GoBack()
    {
        return _navigator.NavigateBackAsync(this);
    }
```


- Add button to XAML and x:Bind Click to GoBack method
```xml
<Button Content="Go Back (View Model)"
                    Click="{x:Bind ViewModel.GoBack}" />
```


- Many other overloads for Navigation depending on the scenario
eg....