---
uid: Uno.Extensions.Markup.HowToCreateMarkupProject
---

# How to set up your own C# Markup project

In this tutorial, you will learn how to set up a new *basic project* using Uno Platform and C# Markup and using some basic resources and also make a comparison between C# Markup and XAML.

The purpose of this section of the tutorial is to show you how to create a basic project from scratch.

## Set up a Markup project

You can use this tutorial to learn how to set up a Uno Platform project.
We will use the comparison between two projects, one being C# Markup and the second XAML.

> The tutorials below can teach you how to create both projects.

- [Setting up the environment and creating the Markup project](xref:Uno.Extensions.HowToMarkupProject)

- [Setting up the environment and creating the XAML project](xref:Uno.Extensions.HowToXamlProject)

## Comparing the structures

Open the two projects created and compare the structure of the two.

### Comparing the C# Markup with the XAML project

- Comparing projects structure

  #### [**C# Markup**](#tab/cs)

  ##### C# Markup project structure

  - The project is separated into Solution Items, where general settings and properties files are located, and Source, which contains information about the Backend, Platforms and Shared Project.

  - Check out how your first Start Up Markup project will look like.

    ![Screenshot showing how to check your project's Markup initial structure using Uno Platform within Visual Studio.](../Assets/MarkupProject-InitialProject.jpg)

  - Set as Startup Project the some Platform and Run the Project.

  - In the Shared Project open the file *MainPage.cs* and analyze the code that will look like this.

    ![Screenshot displaying MainPage using C# Markup in the generated project](../Assets/MarkupProject-GeneratedMarkup.jpg)

  #### [**XAML**](#tab/cli)

  ##### XAML project structure

  - The project is separated into Solution Items, where general settings and properties files are located, and Source, which contains information about the Backend, Platforms and Shared Project.

  - Check out how your first Start Up XAML project will look like.

    ![Screenshot showing how to check your project's XAML initial structure using Uno Platform within Visual Studio.](../Assets/MarkupProject-InitialProjectXAML.jpg)

  - Set as Startup Project the some Platform and Run the Project.

  - In the Shared Project open the file *MainPage.xaml* and analyze the code that will look like this.

    ![Screenshot displaying MainPage using XAML in the generated project](../Assets/MarkupProject-GeneratedXAML.jpg)

## Start creating some basic UI with C# Markup

Change the *MainPage.cs* to have a different content as the sample bellow.

### Add elements and set attributes on the UI

- Customizing the UI

  #### [**C# Markup**](#tab/cs)

  ##### C# Markup

  - The code below shows how to create simple elements like TextBlock in the UI.
    Working as simple as that `new TextBlock()`.

    ```csharp
    //Add Some TextBlock
    new TextBlock()
        .Text("Hello Uno Platform!")
        .Padding(50)//Set some padding
        .Margin(50)//Set some Margin
        .HorizontalAlignment(HorizontalAlignment.Right)//Custom the Alignment
    )
    ```

  #### [**XAML**](#tab/cli)

  ##### XAML

  ```xml

  <TextBlock Text="Hello Uno Platform"
             Padding="50"
             Margin="50"
             HorizontalAlignment="Right" />
  ```

  #### [**Full Code**](#tab/code)

  ##### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

    ```csharp
    namespace MySampleProject;

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
                    new Grid()
                        .Children(
                            //Add Some TextBlock
                            new TextBlock()
                                .Text("Hello Uno Platform!")
                                .Padding(50)//Set some padding
                                .Margin(50)//Set some Margin
                                .HorizontalAlignment(HorizontalAlignment.Right)//Custom the Alignment
                            )
            ));
        }
    }
    ```

### Set the background in different ways

- Changing Background

  #### [**C# Markup**](#tab/cs)

  ##### C# Markup

  - The code below shows how Set the backgroun in 3 different ways.

  - First - accessing the Application Theme

    ```csharp
    new Grid()
        //Set Background from Theme
        .Background(ThemeResource.Get<Brush>("ApplicationSecondaryForegroundThemeBrush"))
        .Children(

            new TextBlock()
                .Text("Hello Uno Platform!")
                .Padding(50)//Set some padding
                .Margin(50)//Set some Margin
                .HorizontalAlignment(HorizontalAlignment.Right)//Custom the Alignment

        ),
    ```

    - Second - using the Colors Helper

    ```csharp

    new Grid()
        //Set Background from Colors
        .Background(new SolidColorBrush(Colors.Silver))
        .Children(

            new TextBlock()
                .Text("Hello Uno Platform!")
                .Padding(50)//Set some padding
                .Margin(50)//Set some Margin
                .HorizontalAlignment(HorizontalAlignment.Right)//Custom the Alignment

        )
    ```

    - Third - creating a new Brush through a manual ARGB color.

    ```csharp
    new Grid()
        //Set Background from custom Brush
        .Background(new SolidColorBrush(Color.FromArgb(255, 233, 233, 233)))
        .Children(

            new TextBlock()
                .Text("Hello Uno Platform!")
                .Padding(50)//Set some padding
                .Margin(50)//Set some Margin
                .HorizontalAlignment(HorizontalAlignment.Right)//Custom the Alignment

        )
    ```

  #### [**XAML**](#tab/cli)

  ##### XAML

  - First - accessing the Application Theme

    ```xml
    <Grid
        Background="{ThemeResource ApplicationSecondaryForegroundThemeBrush}">
        <TextBlock
            Text="Hello Uno Platform"
            Padding="50"
            Margin="50"
            HorizontalAlignment="Right" />
    </Grid>
    ```

    - Second - using the Colors Helper

    ```xml
    <Grid
        Background="Silver">
        <TextBlock
            Text="Hello Uno Platform"
            Padding="50"
            Margin="50"
            HorizontalAlignment="Right" />
    </Grid>
    ```

    - Third - creating a new Brush through a manual ARGB color.

    ```xml
    <Grid
        Background="#FFE9E9E9">
        <TextBlock
            Text="Hello Uno Platform"
            Padding="50"
            Margin="50"
            HorizontalAlignment="Right" />
    </Grid>
    ```

  #### [**Full Code**](#tab/code)

  ##### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

    ```csharp
    using Microsoft.UI;

    namespace MySampleProject;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this
                .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
                .Content(
                    new StackPanel()
                    .VerticalAlignment(VerticalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Children(
                        new Grid()
                            //Set Background from Theme
                            .Background(ThemeResource.Get<Brush>("ApplicationSecondaryForegroundThemeBrush"))
                            .Children(

                                new TextBlock()
                                    .Text("Hello Uno Platform!")
                                    .Padding(50)//Set some padding
                                    .Margin(50)//Set some Margin
                                    .HorizontalAlignment(HorizontalAlignment.Right)//Custom the Alignment

                            ),
                        new Grid()
                            //Set Background from Colors
                            .Background(new SolidColorBrush(Colors.Silver))
                            .Children(

                                new TextBlock()
                                    .Text("Hello Uno Platform!")
                                    .Padding(50)//Set some padding
                                    .Margin(50)//Set some Margin
                                    .HorizontalAlignment(HorizontalAlignment.Right)//Custom the Alignment

                            ),
                        new Grid()
                            //Set Background from custom Brush
                            .Background(new SolidColorBrush(Color.FromArgb(255, 233, 233, 233)))
                            .Children(

                                new TextBlock()
                                    .Text("Hello Uno Platform!")
                                    .Padding(50)//Set some padding
                                    .Margin(50)//Set some Margin
                                    .HorizontalAlignment(HorizontalAlignment.Right)//Custom the Alignment

                            )
            ));
        }
    }

    ```

### Work with the Grid in order to customize the columns and their children

- Changing Grid, RowDefinitions and ColumnDefinitions

  #### [**C# Markup**](#tab/cs)

  ##### C# Markup

  - The code below shows how to create simple Grid element and add the RowDefinitions and ColumnDefinitions.

    ```csharp
    new Grid()
        //Custom the Row and Column Definitions
        .RowDefinitions<Grid>("Auto, *")
        .ColumnDefinitions<Grid>("2*, Auto, 3*")
    ```

    - And how to set the [Attached Properties](xref:Uno.Extensions.Markup.AttachedProperties).

    ```csharp
    new Grid()
        //Custom the Row and Column Definitions
        .RowDefinitions<Grid>("Auto, *")
        .ColumnDefinitions<Grid>("2*, Auto, 3*")

        //Set Background from Theme
        .Children(

            new TextBlock()
                .Padding(50)
                .Grid(row: 0, column: 0)//Set and Attached Properties
                .Text("Row 0"),

            new TextBlock()
                .Margin(50)
                .Grid(grid => grid.Row(0).Column(1).ColumnSpan(2))//Attached Properties using builder pattern
                .Text("Row 0 with ColumnSpan and Attached Properties using builder pattern!"),

            new TextBlock()
                .Margin(50)
                .Grid(row: 1, column: 0)//Set and Attached Properties
                .Text("Row 1 and Column 0"),

            new TextBlock()
                .Margin(50)
                .Grid(row: 1, column: 1)//Set and Attached Properties
                .Text("Row 1 and Column 1"),

            new TextBlock()
                .Margin(50)
                .Grid(grid => grid.Row(1).Column(2))//Attached Properties using builder pattern
                .Text("Row 1 and Column 2")
        )
    ```

  #### [**XAML**](#tab/cli)

  ##### XAML

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" />
            <ColumnDefinition Width="Auto" />
            <ColumnDefinition Width="3*" />
        </Grid.ColumnDefinitions>

        <TextBlock
            Text="Row 0"
            Padding="50"
            Margin="50"
            Grid.Row="0"
            Grid.Column="0"/>

        <TextBlock
            Text="Row 0 with ColumnSpan and Attached Properties using builder pattern!"
            Padding="50"
            Margin="50"
            Grid.Row="0"
            Grid.Column="1"
            Grid.ColumnSpan="2"/>

        <TextBlock
            Text="Row 1 and Column 0"
            Padding="50"
            Margin="50"
            Grid.Row="1"
            Grid.Column="0"/>

        <TextBlock
            Text="Row 1 and Column 1"
            Padding="50"
            Margin="50"
            Grid.Row="1"
            Grid.Column="1"/>

        <TextBlock
            Text="Row 1 and Column 2"
            Padding="50"
            Margin="50"
            Grid.Row="1"
            Grid.Column="2"/>
    </Grid>
    ```

  #### [**Full Code**](#tab/code)

  ##### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

    ```csharp
    namespace MySampleProject;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this
                .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
                .Content(
                    new StackPanel()
                    .VerticalAlignment(VerticalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Children(
                        new Grid()
                            //Custom the Row and Column Definitions
                            .RowDefinitions<Grid>("Auto, *")
                            .ColumnDefinitions<Grid>("2*, Auto, 3*")

                            //Set Background from Theme
                            .Children(

                                new TextBlock()
                                    .Padding(50)
                                    .Grid(row: 0, column: 0)//Set and Attached Properties
                                    .Text("Row 0"),

                                new TextBlock()
                                    .Margin(50)
                                    .Grid(grid => grid.Row(0).Column(1).ColumnSpan(2))//Attached Properties using builder pattern
                                    .Text("Row 0 with ColumnSpan and Attached Properties using builder pattern!"),

                                new TextBlock()
                                    .Margin(50)
                                    .Grid(row: 1, column: 0)//Set and Attached Properties
                                    .Text("Row 1 and Column 0"),

                                new TextBlock()
                                    .Margin(50)
                                    .Grid(row: 1, column: 1)//Set and Attached Properties
                                    .Text("Row 1 and Column 1"),

                                new TextBlock()
                                    .Margin(50)
                                    .Grid(grid => grid.Row(1).Column(2))//Attached Properties using builder pattern
                                    .Text("Row 1 and Column 2")
                            )
                    )
            );
        }
    }
    ```

## Try it yourself

Now try to change your MainPage to have different layout and test other attributes and elements..

We continue in the next section to learn how to configure styles, work with Bindings, Templates and Template Selectors.

## Next Steps

Learn more about:

- [Custom your own C# Markup - Learn how to change Style, Bindings, Templates and Template Selectors using C# Markup](xref:Uno.Extensions.HowToCustomMarkupProject)
- [Custom your own C# Markup - Learn how to change Visual States and User Controls](xref:Uno.Extensions.HowToCustomMarkupProjectVisualStates)
- [Custom your own C# Markup - Learn how to use Toolkit](xref:Uno.Extensions.HowToCustomMarkupProjectToolkit)
- [Custom your own C# Markup - Learn how to Change the Theme](xref:Uno.Extensions.HowToCustomMarkupProjectTheme)
- [Custom your own C# Markup - Learn how to use MVUX](xref:Uno.Extensions.HowToCustomMarkupProjectMVUX)
