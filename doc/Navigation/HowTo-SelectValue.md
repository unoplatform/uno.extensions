# How-To: Select a Value

This topic walks through using Navigation to request a value from the user. For example selecting a value from a list of items. 

> [!Tip] This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions` template to create the solution. Instructions for creating an application from the template can be found [here](../Extensions/GettingStarted/UsingUnoExtensions.md)

## Step-by-steps


- Define a widget class for data to be passed between viewmodels

```csharp
public record Widget(string Name, double Weight){}
```


- Add Widgets property to SecondViewModel

```csharp
public Widget[] Widgets { get; } = new[]
{
    new Widget("NormalSpinner", 5.0),
    new Widget("HeavySpinner",50.0)
};
```

```xml
<ListView ItemsSource="{Binding Widgets}"
            uen:Navigation.Request="-"
            HorizontalAlignment="Center"
            VerticalAlignment="Center">
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

- Change navigate method to request result

```csharp
var widget= await _navigator.NavigateViewModelForResultAsync<SecondViewModel, Widget>(this).AsResult();
```

- Can now select a value from second page that will automatically get returned to mainviewmodel
