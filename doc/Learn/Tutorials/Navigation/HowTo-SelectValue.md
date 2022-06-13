---
uid: Learn.Tutorials.Navigation.HowToSelectValue
---
# How-To: Select a Value

This topic walks through using Navigation to request a value from the user. For example selecting a value from a list of items.

> [!TIP]
> This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions-net6` template to create the solution. Instructions for creating an application from the template can be found [here](../Extensions/GettingStarted/UsingUnoExtensions.md)

> [!IMPORTANT]
> The `unoapp-extensions-net6` template requires the following changes for this tutorial:
>
> 1. Add the following inside the `MainPage` class in `MainPage.xaml.cs`:
>
>```csharp
>    public MainViewModel? ViewModel => DataContext as MainViewModel;
>```
>
> 2. Replace `Content="Go to Second Page"` with `Click="{x:Bind ViewModel.GoToSecondPage}"` in `MainPage.xaml`

## Step-by-steps

### 1. GetDataAsync

This scenario will use Navigation to navigate to a page in order for the user to select an item. The item will be returned via Navigation to the calling code.

- Define a `Widget` record in the class library for data to be passed during navigation

    ```csharp
    public record Widget(string Name, double Weight);
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

- Update `MainViewModel` to use the `GetDataAsync` method in the `GoToSecondPage` method.

    ```csharp
    public async Task GoToSecondPage()
    {
     var widget = await _navigator.GetDataAsync<SecondViewModel, Widget>(this);
    }
    ```

- The selected a value from the `ListView` on `SecondPage` will get returned to the `widget` variable in `MainViewModel`.

### 2. Navigating ForResult Type

In the preceding step the `GetDataAsync` method call specified two generic arguments, `SecondViewModel` which defines the view to navigate to, and `Widget` which defines the type of data to be returned. The `ViewMap` can be updated to define an association between the view and the type of data being requested.

- Update the `ViewMap` for `SecondViewModel` as follows:

    ```csharp
    new ViewMap<SecondPage, SecondViewModel>(ResultData: typeof(Widget))
    ```

- Update the `GetDataAsync` method call to only specify the result data type, `Widget`.

    ```csharp
    public async Task GoToSecondPage()
    {
     var widget = await _navigator.GetDataAsync<Widget>(this);
    }
    ```

Navigation is able to resolve which view to navigate to based on the type of data being requested, in this case `Widget`.
