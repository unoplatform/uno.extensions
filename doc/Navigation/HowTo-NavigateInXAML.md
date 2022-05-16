# How-To: Navigate in XAML

- Navigation can be triggered from XAML based on:
    - tap an element
    - click on a button
    - select item from list

- Add new page, SamplePage.xaml
- Add a button "Go to Sample Page" and in the XAML  

**C#**  
```xml
<Button Content="Go to Sample Page"
        uen:Navigation.Request="Sample" />
```

- On SamplePage add a button "Go back" and in XAML

**C#**  
```xml
<Button Content="Go Back"
        uen:Navigation.Request="-" />
```

- Note that whilst this works, it relies on reflection to convert the request path "Sample" to the corresponding view, ie SamplePage. Better to define viewmap and routemap

- Define SampleViewModel

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


- We can also pass data

- Define a widget class for data to be passed between viewmodels

```csharp
public record Widget(string Name, double Weight){}
```

- Add Widgets property to MainViewModel

```csharp
public Widget[] Widgets { get; } = new[]
{
    new Widget("NormalSpinner", 5.0),
    new Widget("HeavySpinner",50.0)
};
```

- Add XAML to MainPage

```xml
<ListView ItemsSource="{Binding Widgets}" x:Name="WidgetsList">
    <ListView.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal"
                        Padding="10">
                <TextBlock Text="{Binding Name}" />
                <TextBlock Text="{Binding Age}" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```


**C#**  
```xml
<Button Content="Go to Sample Page"
        uen:Navigation.Request="Sample" 
        uen:Navigation.Data="{Binding SelectedItem, ElementName=WidgetsList}"/>
```


- Update SecondViewModel to received a widget

```csharp
public class SampleViewModel
{
    public string Title => "Sample Page";
    private readonly INavigator _navigator;

    public string Name { get; }

    public SampleViewModel(INavigator navigator, Widget widget)
    {
        _navigator = navigator;
        Name = widget.Name;
    }
}

```


```xml
<TextBlock HorizontalAlignment="Center"
                   VerticalAlignment="Center"><Run Text="Widget Name:" /><Run Text="{Binding Name}" /></TextBlock>
```     


new ViewMap<SamplePage, SampleViewModel>(Data: new DataMap<Widget>())


- Can simplify by attaching Navigation.Request to the ListView - don't need to specify the Data property

```csharp
<ListView ItemsSource="{Binding Widgets}"
            uen:Navigation.Request="Sample">
    <ListView.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal"
                        Padding="10">
                <TextBlock Text="{Binding Name}" />
                <TextBlock Text="{Binding Age}" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

- Can simplify further to Request="" - uses data type to determine where to navigate to.




