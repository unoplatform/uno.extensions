---
uid: Uno.Extensions.Navigation.Data.Widgets-inline
---
<!-- markdownlint-disable MD041 MD046-->

    ```csharp
    public Widget[] Widgets { get; } =
    [
        new Widget("NormalSpinner", 5.0),
        new Widget("HeavySpinner", 50.0)
    ];
    ```

- Replace the `Button` with a `ListView` in `MainPage.xaml` that has the `ItemsSource` property data bound to the `Widgets` property.

    ```xml
    <StackPanel Grid.Row="1"
                HorizontalAlignment="Center"
                VerticalAlignment="Center">
        <ListView ItemsSource="{Binding Widgets}"
                  x:Name="WidgetsList"
                  SelectionMode="Single">
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
    </StackPanel>
    ```

- Add a `Button` to the `StackPanel` on  `MainPage.xaml` that has both `Navigation.Request`, that specified the navigation route, and the `Navigation.Data` properties. In this case the `Navigation.Data` attached property is data bound to the `SelectedItem` property on the named element `WidgetsList` (which matches the `x:Name` set on the previously added `ListView`)

    ```xml
    <Button Content="Go to Sample Page"
            uen:Navigation.Request="Sample"
            uen:Navigation.Data="{Binding SelectedItem, ElementName=WidgetsList}"/>
    ```
