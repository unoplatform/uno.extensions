# How-To: Select a Value

This topic walks through using Navigation to request a value from the user. For example selecting a value from a list of items. 

> [!TIP]
> This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions` template to create the solution. Instructions for creating an application from the template can be found [here](../Extensions/GettingStarted/UsingUnoExtensions.md)

## Step-by-steps

This scenario will use Navigation to navigate to a page in order to select an item, which will be returned to the calling code. 

- Define a `Widget` class for data to be passed during navigation

    ```csharp
    public record Widget(string Name, double Weight){}
    ```

- Add `Widgets` property to `SecondViewModel` to define the list of items to pick from.

    ```csharp
    public Widget[] Widgets { get; } = new[]
    {
        new Widget("NormalSpinner", 5.0),
        new Widget("HeavySpinner",50.0)
    };
    ```
- Add a `ListView` to `SecondPage.xaml` to display the items returned by the `Widgets` property. The `Navigation.Request` property specifies a route of `-` which will navigate back to the previous page.
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

- Update `MainViewModel` to use the `NavigateViewModelForResultAsync` method. The `AsResult` extension method is used to access the result value (rather than simply waiting for navigation to complete).
    
    ```csharp
    var widget= await _navigator.NavigateViewModelForResultAsync<SecondViewModel, Widget>(this).AsResult();
    ```

- The selected a value from the `ListView` on `SecondPage` will get returned to the `widget` variable in `MainViewModel`.
