---
uid: Uno.Extensions.Markup.HowToCustomMarkupProjectToolkit
---

# Getting Started with Uno Toolkit

In the previous session we learned how to [Create your own C# Markup](xref:Uno.Extensions.HowToCreateMarkupProject) and how [Custom your own C# Markup - Learn how to change Style, Bindings, Templates and Template Selectors using C# Markup](xref:Uno.Extensions.HowToCustomMarkupProject).

Now we will check [Uno Toolkit](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/getting-started.html) which is a library that can be added to any new or existing Uno solution.

For this sample you can use the how to [Create your own C# Markup with Toolkit](xref:Uno.Extensions.Markup.HowTo-MarkupProjectToolkit)

For this sample we will cover this controls:

- [NavigationBar](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/NavigationBar.html)  - Represents a specialized app bar that provides layout for AppBarButton and navigation logic.

- [Chip](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/ChipAndChipGroup.html) - Chips are compact elements that represent an input, attribute, or action.

## NavigationBar

The NavigationBar is a user interface component used to provide navigation between different pages or sections of an application.

The navigation bar can include items such as a back button, a page title, and other navigation elements, depending on your application's structure and requirements.

### Changing UI to have the [NavigationBar](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/NavigationBar.html)

- In the Shared Project open the file *MainPage.cs* and change the content to have the NavigationBar.

  #### [**C# Markup**](#tab/cs)

  ##### C# Markup

  To do so, create a new class file named SecondPage.
  Then open the class and replace the content for:

    ```csharp
    namespace MySampleToolkitProject;

    public sealed partial class SecondPage : Page
    {
        public SecondPage()
        {
            this
                .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
                .Content(new StackPanel()
                .VerticalAlignment(VerticalAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Children(
                    new NavigationBar().Content("Title Second Page"),
                    new Button()
                        .Content("Go to Main Page")
                        .Name(out var navigationButton)
                ));

            navigationButton.Click += (s, e) =>
            {
                Frame.Navigate(typeof(MainPage));
            };
        }
    }

    ```

  Now we can change the content of the MainPage.cs to have the NavigationBar.

    ```csharp
    new NavigationBar().Content("Title Main Page")
    ```

  And For have the controls to navigate for the SecondPage we can add a Button for do it.

    ```csharp
    new Button()
        .Content("Go to Second Page")
        .Name(out var navigationButton),
    ```

  The main idea here is to create a button that has to assign itself, through the `Name` extension method, the output variable named in this case is `navigationButton`.
  After that, we need to add a Click EventHandler to add the Navigation using the Frame.Navigate.

  > Notice how simple it is to create an action for the Button's Click event.

    ```csharp
    
        navigationButton.Click += (s, e) =>
        {
            Frame.Navigate(typeof(SecondPage));
        };
    ```

  #### [**XAML**](#tab/cli)

  ##### XAML

  To do so, create a new Page file named SecondPage.
  Then open the Page.xaml and replace the content for:

    ```xml
    <Page x:Class="MySampleToolkitProjectXAML.SecondPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:MySampleToolkitProjectXAML"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        xmlns:utu="using:Uno.Toolkit.UI"
        Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

        <StackPanel HorizontalAlignment="Center"
                    VerticalAlignment="Center">
            <utu:NavigationBar Content="Title Main Page"/>
            <Button x:Name="navigationButton" Content="Go to Second Page" Click="navigationButton_Click"/>
        </StackPanel>
    </Page>
    ```

  And the SecondPage.cs

    ```csharp
    namespace MySampleToolkitProjectXAML;

    public sealed partial class SecondPage : Page
    {
        public SecondPage()
        {
            this.InitializeComponent();
        }
        public void navigationButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
    ```

  Now we can change the content of the MainPage.xaml to have the NavigationBar.

    ```xaml
    <utu:NavigationBar Content="Title Main Page"/>
    ```

  And For have the controls to navigate for the SecondPage we can add a Button for do it.

    ```csharp
    <Button 
            Content="Go to Second Page" 
            Click="navigationButton_Click"/>
    ```

  #### [**Full Code**](#tab/code)

  ##### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

    ```csharp
    namespace MySampleToolkitProject;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this
                .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
                .Content(new StackPanel()
                .VerticalAlignment(VerticalAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Children(
                    new NavigationBar().Content("Title Main Page"),
                    new Button()
                        .Content("Go to Second Page")
                        .Name(out var navigationButton)
                ));

            navigationButton.Click += (s, e) =>
            {
                Frame.Navigate(typeof(SecondPage));
            };
        }
    }
    ```

  ##### Full XAML code

  Look how the MainPage.xaml and the MainPage.xaml.cs will like like.

    ```csharp
    namespace MySampleToolkitProjectXAML;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        public void navigationButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SecondPage));
        }
    }

    ```

    ```xml
    <Page x:Class="MySampleToolkitProjectXAML.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:MySampleToolkitProjectXAML"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        xmlns:utu="using:Uno.Toolkit.UI"
        Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

        <StackPanel HorizontalAlignment="Center"
                    VerticalAlignment="Center">
            <utu:NavigationBar Content="Title Main Page"/>
            <Button 
                Content="Go to Second Page" 
                Click="navigationButton_Click"/>
        </StackPanel>
    </Page>

    ```

## [Chip](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/ChipAndChipGroup.html)

A Chip is a user interface component used to display a specific selection or action in a compact format.

Chips are often used to display short pieces of information such as tags, categories, filters, or selected options. They provide a compact and interactive visual representation of this information, allowing users to efficiently view and interact with it.

### Changing UI to have the Chip

- In the Shared Project open the file *MainPage.cs* and change the content to have the Chip.

  #### [**C# Markup**](#tab/cs)

  ##### C# Markup

  Chips can be displayed in lists, dropdown menus, action bars, or other areas of the user interface depending on the application's needs. They are highly flexible and adaptable, allowing you to use them according to your application's interaction flow.

  For this case we will add the Chip to show some information on the UI.

    ```csharp
    new ChipGroup()
        .Name(out var chipGroup)
        .Items(
            new Chip()
                .Margin(5)
                .Content("Go to Second Page")
                .Background(new SolidColorBrush(Colors.LightBlue))
                .Name(out var navigationChip),
        )
    ```

  You can customize the appearance and behavior of Chips by setting properties such as Content, Icon, IsSelected, and handling associated events such as Click or Selected.

  So lets add more Chip to see they in action.
  The Second is for show how to custom the style of the chip.

    ```csharp
    new ChipGroup()
        .Name(out var chipGroup)
        .Items(
            new Chip()
                .Margin(5)
                .Content("Chip 2")
                .Style(new Style<Chip>()
                            .Setters( s => s.Foreground(new SolidColorBrush(Colors.Red)))
                    ),
        )
    ```

  And the third one is for show how to handle the Checked and the Unchecked events.

    ```csharp
    new ChipGroup()
        .Name(out var chipGroup)
        .Items(
            
            new Chip()
                .Margin(5)
                .Content("Chip 3")
                .Name(out var chipElement)
        )
    ```

  And we need to add the event handlers to our code.
  Notice that we are using the `Name` extension method to have access to the chipElement in other places.

    ```csharp
    
    chipElement.Checked += (sender, e) =>
    {
        if (sender is Chip chip)
        {
            chip.FontSize(18);
        }
    };
    chipElement.Unchecked += (sender, e) =>
    {
        if (sender is Chip chip)
        {
            chip.FontSize(14);
        }
    };
    ```

  > Notice that we are able to use Event handlers and use them for many other purposes.

  #### [**XAML**](#tab/cli)

  ##### XAML

  For this case we will add the Chip to show some information on the UI.

    ```xml
    <utu:ChipGroup x:Name="chipGroup">
        <utu:Chip Margin="5" Content="Chip 1" Background="LightBlue" />
    </utu:ChipGroup>
    ```

  You can customize the appearance and behavior of Chips by setting properties such as Content, Icon, IsSelected, and handling associated events such as Click or Selected.

  So lets add more Chip to see they in action.
  The Second is for show how to custom the style of the chip.

    ```xml
    <utu:ChipGroup x:Name="chipGroup">
        <utu:Chip Margin="5" Content="Chip 2">
            <utu:Chip.Style>
                <Style TargetType="utu:Chip">
                    <Setter Property="Foreground" Value="Red" />
                </Style>
            </utu:Chip.Style>
        </utu:Chip>
    </utu:ChipGroup>
    ```

  And the third one is for show how to handle the Checked and the Unchecked events.

    ```xml
    <utu:ChipGroup x:Name="chipGroup">
        <utu:Chip Margin="5" Content="Chip 3" x:Name="chipElement" Checked="chip_Checked" Unchecked="chip_Unchecked"/>
    </utu:ChipGroup>
    ```

  And we need to add the event handlers  on our code.

    ```csharp
    public void chip_Unchecked(object sender, RoutedEventArgs e)
    {
        chipElement.FontSize = 14;
    }
    public void chip_Checked(object sender, RoutedEventArgs e)
    {
        chipElement.FontSize = 18;
    }
    ```

  #### [**Full Code**](#tab/code)

  ##### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

    ```csharp
    using Microsoft.UI;
    using Uno.Toolkit.UI;

    namespace MySampleToolkitProject;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this
                .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
                .Resources(
                        r => r
                            .Add("Icon_Check", "F1 M 4.192500114440918 7.934999942779541 L 1.0649998784065247 4.807499885559082 L 0 5.864999771118164 L 4.192500114440918 10.057499885559082 L 13.192500114440918 1.057499885559082 L 12.135000228881836 0 L 4.192500114440918 7.934999942779541 Z")
                )
                .Content(
                    new StackPanel()
                        .Children(
                            new NavigationBar().Content("Title Main Page")
                                .VerticalAlignment(VerticalAlignment.Top)
                                .HorizontalAlignment(HorizontalAlignment.Left),
                            new StackPanel()
                                .Margin(0,50,0,0)
                                .VerticalAlignment(VerticalAlignment.Center)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Children(
                                    new Button()
                                        .Content("Go to Second Page")
                                        .Name(out var navigationButton),
                                    new ChipGroup()
                                        .Name(out var chipGroup)
                                        .Items(
                                            new Chip()
                                                .Margin(5)
                                                .Content("First Chip")
                                                .Background(new SolidColorBrush(Colors.LightBlue)),
                                            new Chip()
                                                .Margin(5)
                                                .CanRemove(true)
                                                .Content("Chip 2")
                                                .Name(out var chipRemoveElement)
                                                .Style(new Style<Chip>()
                                                            .Setters(s => s.Foreground(new SolidColorBrush(Colors.Red)))
                                                    ),
                                            new Chip()
                                                .Margin(5)
                                                .Content("Chip 3")
                                                //.Icon(new PathIcon().Data(StaticResource.Get<Geometry>("Icon_Check")))
                                                //.Icon(new SymbolIcon(Symbol.Favorite))
                                                .Name(out var chipElement)
                                        )
                                )
                        )
                );

            navigationButton.Click += (s, e) =>
            {
                Frame.Navigate(typeof(SecondPage));
            };


            chipElement.Checked += (sender, e) =>
            {
                if (sender is Chip chip)
                {
                    chip.FontSize(18);
                }
            };
            chipElement.Unchecked += (sender, e) =>
            {
                if (sender is Chip chip)
                {
                    chip.FontSize(14);
                }
            };
        }
    }

    ```

  ##### Full XAML code

  - MainPage.xaml

    ```xml
    <Page x:Class="MySampleToolkitProjectXAML.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:MySampleToolkitProjectXAML"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        xmlns:utu="using:Uno.Toolkit.UI"
        Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

        <StackPanel>
            <utu:NavigationBar Content="Title Main Page"
                        HorizontalAlignment="Left"
                        VerticalAlignment="Top"/>
            <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center">
                <Button Content="Go to Second Page" Click="navigationButton_Click"/>
        
                <utu:ChipGroup x:Name="chipGroup">
                    <utu:Chip Margin="5" Content="Chip 1" Background="LightBlue" />
                    <utu:Chip Margin="5" Content="Chip 2">
                        <utu:Chip.Style>
                            <Style TargetType="utu:Chip">
                                <Setter Property="Foreground" Value="Red" />
                            </Style>
                        </utu:Chip.Style>
                    </utu:Chip>
                    <utu:Chip Margin="5" Content="Chip 3" x:Name="chipElement" Checked="chip_Checked" Unchecked="chip_Unchecked"/>

                </utu:ChipGroup>
            </StackPanel>
        </StackPanel>
    </Page>
    ```

  - MainPage.xaml.cs

    ```csharp
    namespace MySampleToolkitProjectXAML;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        public void navigationButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SecondPage));
        }
        public void chip_Unchecked(object sender, RoutedEventArgs e)
        {
            chipElement.FontSize = 14;
        }
        public void chip_Checked(object sender, RoutedEventArgs e)
        {
            chipElement.FontSize = 18;
        }
    
    }
    ```

## Try it yourself

Now try to change your MainPage to have different layout and test other attributes and elements.

In this Tutorial we add the NavigationBar and some Chip to the UI.

But the Uno Toolkit has many other Controls as:

- [AutoLayout](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/AutoLayoutControl.html)
- [Cards](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/CardAndCardContentControl.html)
- [Chip and ChipGroup](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/ChipAndChipGroup.html)
- [DrawerControl](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/DrawerControl.html)
- [DrawerFlyoutPresenter](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/DrawerFlyoutPresenter.html)
- [LoadingView](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/LoadingView.html)
- [NavigationBar](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/NavigationBar.html)
- [SafeArea](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/SafeArea.html)
- [TabBar and TabBarItem](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/TabBarAndTabBarItem.html)

> Try to add another control as a learning exercise.

## Next Steps

- [Custom your own C# Markup - Learn how to change Visual States and User Controls](xref:Uno.Extensions.HowToCustomMarkupProjectVisualStates)
- [Custom your own C# Markup - Learn how to Change the Theme](xref:Uno.Extensions.HowToCustomMarkupProjectTheme)
- [Custom your own C# Markup - Learn how to use MVUX](xref:Uno.Extensions.HowToCustomMarkupProjectMVUX)
