---
uid: Learn.Tutorials.Navigation.Advanced.Panel
---
# How-To: Use a Panel to Switch Views

> [!WARNING]
> **Work in progress:** 
>This page is currently under construction. It will be available soon. 🚧
>
> **Have questions or feedback?**
>You can help shape the documentation for this topic by providing feedback on the Uno.Extensions [repo](https://github.com/unoplatform/uno.extensions/discussions/categories/general)

> [!TIP]
> This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions` template to create the solution. Instructions for creating an application from the template can be found [here](xref:Overview.Extensions)

## Step-by-steps

### Code example

#### Navigating using a Panel

    ```xml
    <Page x:Class="UsingPanelRegion.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:local="using:UsingPanelRegion.Views"
          xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
          xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
          mc:Ignorable="d"
          xmlns:uen="using:Uno.Extensions.Navigation.UI"
          xmlns:utu="using:Uno.Toolkit.UI"
          Background="{ThemeResource MaterialBackgroundBrush}">

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
                <RowDefinition />
            </Grid.RowDefinitions>

            <utu:NavigationBar Content="Main Page" 
                               Style="{StaticResource MaterialNavigationBarStyle}"/>

            <StackPanel Grid.Row="1" 
                        HorizontalAlignment="Center" 
                        Orientation="Horizontal">
                <Button Content="One"
                        uen:Navigation.Request="./One" />
                <Button Content="Two"
                        uen:Navigation.Request="./Two" />
                <Button Content="Three"
                        uen:Navigation.Request="./Three" />
            </StackPanel>

            <Grid uen:Region.Attached="True" 
                  uen:Region.Navigator="Visibility" 
                  Grid.Row="2">
                <Grid uen:Region.Name="One"
                      Visibility="Collapsed">
                    <TextBlock Text="One" 
                               FontSize="24" 
                               HorizontalAlignment="Center" 
                               VerticalAlignment="Center"/>
                </Grid>
                <Grid uen:Region.Name="Two"
                      Visibility="Collapsed">
                    <TextBlock Text="Two"
                               FontSize="24"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center" />
                </Grid>
                <Grid uen:Region.Name="Three"
                      Visibility="Collapsed">
                    <TextBlock Text="Three"
                               FontSize="24"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center" />
                </Grid>
            </Grid>
        </Grid>
    </Page>
    ```

- Show how to use a Grid to switch between content
