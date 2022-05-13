# How-To: Display Item Details

- Define a widget class for data to be passed between viewmodels

```csharp
public record Widget(string Name, double Weight){}
```

- Change Second viewap to include datamap

```csharp
new ViewMap<SecondPage, SecondViewModel>(Data: new DataMap<Widget>())
```

- Change MainViewModel to create a widget and pass as data in navigation method
       
```csharp
public async Task GoToSecondPage()
{
	var widget = new Widget("CrazySpinner", 34.0);

	await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data:widget);
}
```

- Update SecondViewModel to received a widget

```csharp
public class SecondViewModel
{
    public string Name { get; }
    
    public SecondViewModel(Widget widget)
	{
        Name = widget.Name; 
	}
}
```

- Add TextBlock to SecondPage xaml

```xml
<TextBlock HorizontalAlignment="Center"
                   VerticalAlignment="Center"><Run Text="Widget Name:" /><Run Text="{Binding Name}" /></TextBlock>
```          
- Show (screenshot) app on second page showing widget name            
            
- Because there's a mapping between the Widget and the SecondViewModel, can just navigate to show data. Change navigate method to NavigateDataAsync

```csharp
await _navigator.NavigateDataAsync(this, data: widget);
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
<ListView ItemsSource="{Binding Widgets}"
            uen:Navigation.Request="Second">
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
 
- Note that it's the route in the Navigation.Request that defines which view to open

- Change the  request to "" in order for the view to be based on the data type to open)

- Add Basic and Advanced Widgets and change Widgets to be a mix of widget types

```csharp
public record Widget(string Name, double Weight) { }

public record BasicWidget(string Name, double Weight) : Widget(Name, Weight) { }

public record AdvancedWidget(string Name, double Weight) : Widget(Name, Weight) { }


public Widget[] Widgets { get; } = new Widget[]
{
    new BasicWidget("NormalSpinner", 5.0),
    new AdvancedWidget("HeavySpinner",50.0)
};
```

- Copy SecondPage to ThirdPage (rename both xaml and cs files)
- Copy SecondViewModel to ThirdViewModel
- Change constructor of SecondViewModel to accept BasicWidget
- Change constructor of ThirdViewModel to accept AdvancedWidget

- Change viewmap and routemap to include thirdviewmodel

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap<ShellControl, ShellViewModel>(),
        new ViewMap<MainPage, MainViewModel>(),
        new ViewMap<SecondPage, SecondViewModel>(Data: new DataMap<BasicWidget>()),
        new ViewMap<ThirdPage, ThirdViewModel>(Data: new DataMap<AdvancedWidget>())
        );

    routes
        .Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                    Nested: new RouteMap[]
                    {
                                    new RouteMap("Main", View: views.FindByViewModel<MainViewModel>() ,
                                            IsDefault: true
                                            ),
                                    new RouteMap("Second", View: views.FindByViewModel<SecondViewModel>()),
                                    new RouteMap("Third", View: views.FindByViewModel<ThirdViewModel>()),
                    }));
}
```

- Picking an item from the list will either open second or third page




