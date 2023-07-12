---
uid: Reference.Markup.HowToCustomMarkupProjectMVUX
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




## Start creating some basic UI with C# Markup

Change the *MainPage.cs* to have a different content as the sample bellow.

### Add elements and set attributes on the UI.

- Customizing the UI

    # [**C# Markup**](#tab/cs)

    #### C# Markup.

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

    # [**XAML**](#tab/cli)
    
    #### XAML

    ```xml

    <TextBlock 
	    Text="Hello Uno Platform"
	    Padding="50"
	    Margin="50"
	    HorizontalAlignment="Right" />

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

## Next Steps

- [Custom your own C# Markup - Learn how to change Visual States and User Controls](xref:Reference.Markup.HowToCustomMarkupProjectVisualStates)
- [Custom your own C# Markup - Learn how to use Toolkit](xref:Reference.Markup.HowToCustomMarkupProjectToolkit)
- [Custom your own C# Markup - Learn how to Change the Theme](xref:Reference.Markup.HowToCustomMarkupProjectTheme)
- [Custom your own C# Markup - Learn how to use MVUX](xref:Reference.Markup.HowToCustomMarkupProjectMVUX)

Now start to create your own project using C# Markup from Uno Platform.

- [C# Markup documentation](xref:Reference.Markup.GettingStarted)
