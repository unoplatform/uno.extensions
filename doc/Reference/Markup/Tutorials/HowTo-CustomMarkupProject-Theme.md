---
uid: Reference.Markup.HowToCustomMarkupProjectTheme
---

# How to set up your own C# Markup project

In this tutorial you will learn how to set up a new *basic project* using Uno Platform and C# Markup and using some basic resources and also make a comparison between C# Markup and XAML.

The purpose of this section of the tutorial is to show you how to create a basic project from scratch.

## Set up a Markup project

You can use this tutorial to learn how to set up a Uno Platform project.
We will use the comparison between two projects, one being C# Markup and the second XAML.

> The tutorials below can teach you how to create both projects.

- [Setting up the environment and creating the Markup project](xref:Reference.Markup.HowToMarkupProject)

- [Setting up the environment and creating the XAML project](xref:Reference.Markup.HowToXamlProject)

## Comparing the structures

Open the two projects created and compare the structure of the two.




### Work with the Grid in order to customize the columns and their children.

- Changing Grid, RowDefinitions and ColumnDefinitions

    # [**C# Markup**](#tab/cs)

    #### C# Markup

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

    # [**XAML**](#tab/cli)
    
    #### XAML

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

    # [**Full Code**](#tab/code)
    #### Full C# Markup code
    
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

- [Custom your own C# Markup - Learn how to change Visual States and User Controls](xref:Reference.Markup.HowToCustomMarkupProjectVisualStates)
- [Custom your own C# Markup - Learn how to use Toolkit](xref:Reference.Markup.HowToCustomMarkupProjectToolkit)
- [Custom your own C# Markup - Learn how to Change the Theme](xref:Reference.Markup.HowToCustomMarkupProjectTheme)
- [Custom your own C# Markup - Learn how to use MVUX](xref:Reference.Markup.HowToCustomMarkupProjectMVUX)
