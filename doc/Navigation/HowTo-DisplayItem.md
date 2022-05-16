# How-To: Display Item Details

This topic walks through how to use Navigation to display the details of an item selected from a list. This demonstrates an important aspect of Navigation which is the ability to pass data as part of a navigation request.

> [!Tip] This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions` template to create the solution. Instructions for creating an application from the template can be found [here](../Extensions/GettingStarted/UsingUnoExtensions.md)

## Step-by-steps


- Define a widget class for data to be passed between view models

```csharp
public record Widget(string Name, double Weight){}
```

- Change the `ViewMap` associating the second page and its view model to include a `DataMap`

```csharp
new ViewMap<SecondPage, SecondViewModel>(Data: new DataMap<Widget>())
```

- Create a widget inside the main view model, and pass it as `data` into the navigation method
       
```csharp
public async Task GoToSecondPage()
{
	var widget = new Widget("CrazySpinner", 34.0);

	await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data: widget);
}
```

- Update the second view model constructor to accept a widget

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

- Add a TextBlock to second page XAML

```xml
<TextBlock HorizontalAlignment="Center"
                   VerticalAlignment="Center"><Run Text="Widget Name:" /><Run Text="{Binding Name}" /></TextBlock>
```          
- Show (screenshot) app on second page showing widget name            
            
- Because there's a mapping between the Widget and the SecondViewModel, can just navigate to show data. Change navigate method to NavigateDataAsync

```csharp
await _navigator.NavigateDataAsync(this, data: widget);
```


- Add a `Widgets` property to your main view model, similar to the following:

```csharp
public Widget[] Widgets { get; } = new[]
{
    new Widget("NormalSpinner", 5.0),
    new Widget("HeavySpinner",50.0)
};
```

- Update main page XAML to display widgets

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
 
- NOTE: The route in Navigation.Request defines which view to open

- Change the request to `""` to display a view based on the data type

- Add additional widgets and change the `Widgets` property in the main view model to include multiple widget types

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

- Copy the second page's XAML and code behind to a new third page with a different file name
- Copy the second view model to a newly-created, third view model
- Change constructor of both the second and third view models to accept widgets of different types

- Change `ViewMap` and `RouteMap` to include the third view model

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




