---
uid: Uno.Extensions.Navigation.HowToShowFlyout
---
# How-To: Show a Flyout

This topic walks through using Navigation to display a modal flyout

## Step-by-steps

> [!IMPORTANT]
> This guide assumes you used the template wizard or `dotnet new unoapp` to create your solution. If not, it is recommended that you follow the [instructions](xref:Uno.Extensions.HowToGettingStarted) for creating an application from the template.

### 1. Displaying flyout from code

- Add new `Page`, `SamplePage.xaml`, which will be used to display content inside the flyout.

    ```xml
    <Page x:Class="ShowFlyout.Views.SamplePage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:local="using:ShowFlyout.Views"
          xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
          xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
          mc:Ignorable="d"
          Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
    
        <Grid>
            <TextBlock Text="Flyout content"
                       FontSize="32"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center" />
        </Grid>
    </Page>
    ```

- Update the `Button` in `MainPage.xaml` as follows, which wires up the `Click` event to the `ShowFlyoutClick` method  

    ```xml
    <Button Content="Show flyout"
            Click="ShowFlyoutClick" 
            Grid.Row="1"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"/>
    ```

- Add the `ShowFlyoutClick` method to the `MainPage.xaml.cs` file

    ```csharp
    private void ShowFlyoutClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateViewAsync<SamplePage>(this, qualifier: Qualifiers.Dialog);
    }
    ```

### 2. Displaying flyout from XAML

- Add another `Button` with the content `Show flyout from XAML` to `MainPage.xaml`. Set the `Navigation.Request` property to `!Sample` which indicates the `Sample` route should be opened as a Flyout.  

    ```xml
    <StackPanel Grid.Row="1"
                HorizontalAlignment="Center"
                VerticalAlignment="Center">
        <Button Content="Show flyout"
                Click="ShowFlyoutClick" />
        <Button Content="Show flyout from XAML"
                uen:Navigation.Request="!Sample" />
    </StackPanel>
    ```
