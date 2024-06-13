---
uid: Uno.Extensions.Markup.HowToCustomMarkupProjectVisualStates
---

# Getting Started with UserControl and VisualStatesManager

In the previous session we learn how to [Custom your own C# Markup and Learn how to use Toolkit](xref:Uno.Extensions.Markup.HowToCustomMarkupProjectToolkit).

Now we will learn how to use the [UserControl](https://platform.uno/docs/articles/implemented/windows-ui-xaml-controls-page.html) and the [VisualStateManagers](xref:Uno.Extensions.Markup.VisualStateManager).

For this sample you can use same project we start on the how to [Create your own C# Markup with Toolkit](xref:Uno.Extensions.Markup.HowTo-MarkupProjectToolkit)

## UserControl

A UserControl is a reusable user interface component that allows you to group related visual elements and behavior into a single building block.
It provides a way to create custom components that can be used in multiple parts of the application.

### Changing UI to have the UserControl

- In the Shared Project we need to add a UserControl.
- The purpose of the new UserControl will be to have the Chips in a single place, so we can use the same code on the MainPage and the SecondPage.

  #### [**C# Markup**](#tab/cs)

  ##### C# Markup

  Create a new Uno Platform UserControl.
  On the SharedProject Right click on the project name -> Add -> Class -> Inform the Name `SampleUserControl` and click Add.

  Now we can change the content of the SampleUserControl.cs to have the Chips, copy the ChipGroup from the MainPage.cs to this file..

    ```csharp
    this.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
        .Content(
                new ChipGroup()
                .Name(out var chipGroup)
                .Items(
                    new Chip()
                        .Margin(5)
                        .Content("First Chip")
                        .Background(new SolidColorBrush(Colors.LightBlue))
                        .Name(out var navigationChip),
                    new Chip()
                        .Margin(5)
                        .Content("Chip 2")
                        .Style(new Style<Chip>()
                                    .Setters(s => s.Foreground(new SolidColorBrush(Colors.Red)))
                            ),
                    new Chip()
                        .Margin(5)
                        .Content("Chip 3")
                        .Name(out var chipElement)
                )
            );
    ```

  And move the EventHandler from the MainPage.cs to the SampleUserControl.cs.

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

  And change the ChipGroup on the MainPage to the new UserControl.

    ```csharp
    .Children(
        new Button()
            .Content("Go to Second Page")
            .Name(out var navigationButton),
        new SampleUserControl()
    )
    ```

  We can do the same on the SecondPage, check how will be the full code on the SecondPage

    ```csharp
    namespace MySampleToolkitProject;

    public sealed partial class SecondPage : Page
    {
        public SecondPage()
        {
            this
                .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
                .Content(
                    new StackPanel()
                        .Children(
                            new NavigationBar().Content("Title Second Page")
                                .VerticalAlignment(VerticalAlignment.Top)
                                .HorizontalAlignment(HorizontalAlignment.Left),
                            new StackPanel()
                                .Margin(0, 50, 0, 0)
                                .VerticalAlignment(VerticalAlignment.Center)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Children(
                                    new Button()
                                        .Content("Go to Main Page")
                                        .Name(out var navigationButton),
                                    new SampleUserControl()
                                )
                        )
                );
            navigationButton.Click += (s, e) =>
            {
                Frame.Navigate(typeof(MainPage));
            };
        }
    }
    ```

  #### [**XAML**](#tab/cli)

  ##### XAML

  Create a new Uno Platform UserControl.
  On the SharedProject Right click on the project name -> Add -> New Item... -> Than filter by User Control and select a User Control (Uno Platform) and informe the Name `SampleUserControl` and click Add.

  Now we can change the content of the SampleUserControl.xaml to have the Chips, copy the ChipGroup from the MainPage.xaml to this file..

    ```xml
    <UserControl
        x:Class="MySampleToolkitProjectXAML.SampleUserControl"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:MySampleToolkitProjectXAML"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        xmlns:utu="using:Uno.Toolkit.UI"
        d:DesignHeight="300"
        d:DesignWidth="400">

        <Grid>
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
        </Grid>
    </UserControl>

    ```

  And move the EventHandler from the MainPage.cs to the SampleUserControl.xaml.cs.

    ```csharp
    public sealed partial class SampleUserControl : UserControl
    {
        public SampleUserControl()
        {
            this.InitializeComponent();
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

  And change the ChipGroup on the MainPage to the new UserControl.

    ```xml
    <StackPanel HorizontalAlignment="Center"
        VerticalAlignment="Center">
        <Button Content="Go to Second Page" Click="navigationButton_Click"/>

        <local:SampleUserControl/>

    </StackPanel>
    ```

  We can do the same on the SecondPage, check how will be the full code on the SecondPage

    ```csharp
    <StackPanel>
        <utu:NavigationBar Content="Title Second Page"
                    HorizontalAlignment="Left"
                    VerticalAlignment="Top"/>
        <StackPanel HorizontalAlignment="Center"
            VerticalAlignment="Center">
            <Button Content="Go to Main Page" Click="navigationButton_Click"/>

            <local:SampleUserControl/>
        </StackPanel>
    </StackPanel>
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
                .Content(
                    new StackPanel()
                        .Children(
                            new Button()
                            .Width(40)
                            .Height(40)
                            .Content("Test"),
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
                                    new SampleUserControl()
                                )
                        )
                );

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

        <StackPanel>
            <utu:NavigationBar Content="Title Main Page"
                        HorizontalAlignment="Left"
                        VerticalAlignment="Top"/>
            <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center">
                <Button Content="Go to Second Page" Click="navigationButton_Click"/>

                <local:SampleUserControl/>
            </StackPanel>
        </StackPanel>
    </Page>
    ```

## [VisualStateManagers](xref:Uno.Extensions.Markup.VisualStateManager)

The VisualStateManager is a class that allows you to define and manage visual states for controls or elements.

With VisualStateManager, we can define states such as "Normal", "Pressed","PointerEntered","PointerExited", "Focused," or any other custom states you want to handle.
For each state, you can set various properties, such as background color, font size, visibility, or any other property relevant to the control's appearance or behavior.

### Changing UI to have the VisualState

 We need to add the VisualStateManager to the top element of the page, usually a Grid or StackPanel.
With this we can control events and changes.

- For this sample we will use the VisualStateManager for change the Background color and the Width of some element.

  #### [**C# Markup**](#tab/cs)

  ##### C# Markup

  ###### Add VisualStateManager to the Page

  In the Shared Project open the file *MainPage.cs* and change the content to have the VisualStateManager.

    ```csharp
    .VisualStateManager(builder =>
                builder.Group("ButtonStates",
                    groupBuilder =>
                        groupBuilder
                        .State("PointerEntered",
                            stateBuilder => stateBuilder
                                .Setters(btn, e => e.Width(500))
                                .Setters(navigationButton, e => e.Background(new SolidColorBrush(Colors.Blue)))
                        ).State("PointerExited",
                            stateBuilder => stateBuilder
                                .Setters(btn, e => e.Width(100))
                                .Setters(navigationButton, e => e.Background(new SolidColorBrush(Colors.White)))
                        )
                    )
            )
    ```

  We will change the navigationButton (the same one that has been assigned for controlling the navigation) and change its background color now.
  And change the value of the Width of the Button that will be used on the EventHandler.
  You can add the Button before the `new SampleUserControl()`.

    ```csharp
    new Button()
        .Width(200)
        .Height(40)
        .Content("VisualStateManager Test")
        .Name(out var btn),
    ```

  And to handle the Event Handlers we need to create the Event Handlers so that the events are fired and the actions happen.

    ```csharp

    btn.PointerEntered += (sender, e) =>
    {
        VisualStateManager.GoToState(this, "PointerEntered", true);
    };

    btn.PointerExited += (sender, e) =>
    {
        VisualStateManager.GoToState(this, "PointerExited", true);
    };

    ```

  Now Run the Project and test it.

  ###### Add VisualStateManager to the UserControl

  In the Shared Project open the file *SampleUserControl.cs* and change the content to have the VisualStateManager.
  For the User Control, we will add some new Grid Element attach the VisualStateManager to it.

    ```csharp
    new Grid()
    .RowDefinitions<Grid>("Auto, *")
    .Children(
    ...
    )
    .VisualStateManager(builder =>
                builder.Group("ButtonStates",
                    groupBuilder =>
                        groupBuilder
                        .State("PointerEntered",
                        stateBuilder => stateBuilder
                            .Setters(visualStateButtonChips, e => e.Width(500))
                        ).State("PointerExited",
                            stateBuilder => stateBuilder
                                .Setters(visualStateButtonChips, e => e.Width(200))
                        )
                    )
            )
    ```

  We will change the value of the Width of the Button that will be used on the EventHandler.
  You can add the Button in the Second Row of the new Grid.

    ```csharp
    new Button()
        .Grid(row: 1)
        .Content("Visual State on UserControl")
        .Name(out var visualStateButtonChips)
    ```

  And to handle the Event Handlers we need to create the Event Handlers so that the events are fired and the actions happen.

    ```csharp
    
    visualStateButtonChips.PointerEntered += (sender, e) =>
    {
        VisualStateManager.GoToState(this, "PointerEntered", true);
    };

    visualStateButtonChips.PointerExited += (sender, e) =>
    {
        VisualStateManager.GoToState(this, "PointerExited", true);
    };
    ```

  #### [**XAML**](#tab/cli)

  ##### XAML

  ###### Add VisualStateManager to the Page

  In the Shared Project open the file *MainPage.xaml* and change the content to have the VisualStateManager.

  After the start of the first StackPanel add the XAML below

    ```xml
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name="ButtonStates">
                <VisualState x:Name="CustomPointerEntered">
                    <VisualState.Setters>
                        <Setter Target="btn.Width" Value="500" />
                        <Setter Target="navigationButton.Background" Value="Blue" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="CustomPointerExited">
                    <VisualState.Setters>
                        <Setter Target="btn.Width" Value="200" />
                        <Setter Target="navigationButton.Background" Value="White" />
                    </VisualState.Setters>
                </VisualState>
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
    ```

  We will change the navigationButton (the same that has been used for control the Navigation) and will the Background Color now.
  And change the value of the Width of the Button that will be used on the EventHandler.
  You can add the Button before the `<utu:ChipGroup x:Name="chipGroup">`.

    ```csharp
    <Button  x:Name="btn" Content="VisualStateManager Test" Width="300"/>
    ```

  And to handle the Event Handlers we need to create the Event Handlers so that the events are fired and the actions happen.
  For that open the MainPage.xaml.cs and add the Event Handlers.

    ```csharp
    public MainPage()
    {
        this.InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        btn.PointerEntered += (sender, e) =>
        {
            VisualStateManager.GoToState(this, "CustomPointerEntered", true);
        };

        btn.PointerExited += (sender, e) =>
        {
            VisualStateManager.GoToState(this, "CustomPointerExited", true);
        };
    }

    ```

  Now Run the Project and test it.

  ###### Add VisualStateManager to the UserControl

  In the Shared Project open the file *SampleUserControl.xaml* and change the content to have the VisualStateManager.
  For the User Control, we will use the Grid Element to attach the VisualStateManager.

    ```csharp
    new Grid()
    .RowDefinitions<Grid>("Auto, *")
    .Children(
    ...
    )
    .VisualStateManager(builder =>
                builder.Group("ButtonStates",
                    groupBuilder =>
                        groupBuilder
                        .State("PointerEntered",
                        stateBuilder => stateBuilder
                            .Setters(visualStateButtonChips, e => e.Width(500))
                        ).State("PointerExited",
                            stateBuilder => stateBuilder
                                .Setters(visualStateButtonChips, e => e.Width(200))
                        )
                    )
            )
    ```

  We will change the value of the Width of the Button that will be used on the EventHandler.
  You can add the Button in the Second Row of the new Grid.

    ```xml
    <Button Grid.Row="1" Content="Visual State on UserControl" x:Name="visualStateButtonChips"/>
    ```

  And to handle the Event Handlers we need to create the Event Handlers so that the events are fired and the actions happen.

    ```csharp
    public SampleUserControl()
    {
        this.InitializeComponent();
        Loaded += SampleUserControl_Loaded;
    }

    private void SampleUserControl_Loaded(object sender, RoutedEventArgs e)
    {
        visualStateButtonChips.PointerEntered += (sender, e) =>
        {
            VisualStateManager.GoToState(this, "CustomPointerEntered", true);
        };

        visualStateButtonChips.PointerExited += (sender, e) =>
        {
            VisualStateManager.GoToState(this, "CustomPointerExited", true);
        };
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
                                    new Button()
                                        .Width(200)
                                        .Height(40)
                                        .Content("VisualStateManager Test")
                                        .Name(out var btn),
                                    new SampleUserControl()
                                )
                        )
                        .VisualStateManager(builder =>
                                builder.Group("ButtonStates",
                                    groupBuilder =>
                                        groupBuilder
                                        .State("PointerEntered",
                                            stateBuilder => stateBuilder
                                                .Setters(btn, e => e.Width(500))
                                                .Setters(navigationButton, e => e.Background(new SolidColorBrush(Colors.Blue)))
                                        ).State("PointerExited",
                                            stateBuilder => stateBuilder
                                                .Setters(btn, e => e.Width(200))
                                                .Setters(navigationButton, e => e.Background(new SolidColorBrush(Colors.White)))
                                        )
                                    )
                            )
                );

            navigationButton.Click += (s, e) =>
            {
                Frame.Navigate(typeof(SecondPage));
            };

            btn.PointerEntered += (sender, e) =>
            {
                VisualStateManager.GoToState(this, "PointerEntered", true);
            };

            btn.PointerExited += (sender, e) =>
            {
                VisualStateManager.GoToState(this, "PointerExited", true);
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

            <VisualStateManager.VisualStateGroups>
                <VisualStateGroup x:Name="ButtonStates">
                    <VisualState x:Name="CustomPointerEntered">
                        <VisualState.Setters>
                            <Setter Target="btn.Width" Value="500" />
                            <Setter Target="navigationButton.Background" Value="Blue" />
                        </VisualState.Setters>
                    </VisualState>
                    <VisualState x:Name="CustomPointerExited">
                        <VisualState.Setters>
                            <Setter Target="btn.Width" Value="200" />
                            <Setter Target="navigationButton.Background" Value="White" />
                        </VisualState.Setters>
                    </VisualState>
                </VisualStateGroup>
            </VisualStateManager.VisualStateGroups>

            <utu:NavigationBar Content="Title Main Page"
                        HorizontalAlignment="Left"
                        VerticalAlignment="Top"/>
            <StackPanel HorizontalAlignment="Center"
                VerticalAlignment="Center">

                <Button Content="Go to Second Page" x:Name="navigationButton" Click="navigationButton_Click"/>
            
                <Button  x:Name="btn" Content="VisualStateManager Test" Width="300"/>

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
    using Windows.Devices.Input;

    namespace MySampleToolkitProjectXAML;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            btn.PointerEntered += (sender, e) =>
            {
                VisualStateManager.GoToState(this, "CustomPointerEntered", true);
            };

            btn.PointerExited += (sender, e) =>
            {
                VisualStateManager.GoToState(this, "CustomPointerExited", true);
            };
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

  - SampleUserControl.xaml

    ```xml
    <UserControl
        x:Class="MySampleToolkitProjectXAML.SampleUserControl"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:MySampleToolkitProjectXAML"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        xmlns:utu="using:Uno.Toolkit.UI"
        d:DesignHeight="300"
        d:DesignWidth="400">

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>

            <VisualStateManager.VisualStateGroups>
                <VisualStateGroup x:Name="ButtonStates">
                    <VisualState x:Name="CustomPointerEntered">
                        <VisualState.Setters>
                            <Setter Target="visualStateButtonChips.Width" Value="500" />
                        </VisualState.Setters>
                    </VisualState>
                    <VisualState x:Name="CustomPointerExited">
                        <VisualState.Setters>
                            <Setter Target="visualStateButtonChips.Width" Value="200" />
                        </VisualState.Setters>
                    </VisualState>
                </VisualStateGroup>
            </VisualStateManager.VisualStateGroups>
        
            <utu:ChipGroup x:Name="chipGroup" Grid.Row="0">
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

            <Button Grid.Row="1" Content="Visual State on UserControl" x:Name="visualStateButtonChips"/>

        </Grid>
    </UserControl>
    ```

  - MainPage.xaml.cs

    ```csharp
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Foundation;
    using Windows.Foundation.Collections;
    using Windows.UI.Xaml;


    // The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

    namespace MySampleToolkitProjectXAML
    {
        public sealed partial class SampleUserControl : UserControl
        {
            public SampleUserControl()
            {
                this.InitializeComponent();
                Loaded += SampleUserControl_Loaded;
            }

            private void SampleUserControl_Loaded(object sender, RoutedEventArgs e)
            {
                visualStateButtonChips.PointerEntered += (sender, e) =>
                {
                    VisualStateManager.GoToState(this, "CustomPointerEntered", true);
                };

                visualStateButtonChips.PointerExited += (sender, e) =>
                {
                    VisualStateManager.GoToState(this, "CustomPointerExited", true);
                };
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
    }

    ```

## Try it yourself

Now try to change your MainPage to have different layout and test other attributes and elements.

## Next Steps

- [Custom your own C# Markup - Learn how to Change the Theme](xref:Uno.Extensions.HowToCustomMarkupProjectTheme)
- [Custom your own C# Markup - Learn how to use MVUX](xref:Uno.Extensions.HowToCustomMarkupProjectMVUX)
