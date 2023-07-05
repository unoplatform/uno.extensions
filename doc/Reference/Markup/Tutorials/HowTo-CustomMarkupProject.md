---
uid: Reference.Markup.HowToCustomMarkupProject
---

# How to custom an C# Markup project

- If you haven't already set up your environment and create the new Markup project, please follow the steps to [Create your own C# Markup](xref:Reference.Markup.HowToCreateMarkupProject).

In this tutorial you'll learn how to custom your project using Uno Platform and C# Markup. 

In the previous session we learn how to [Create your own C# Markup](xref:Reference.Markup.HowToCreateMarkupProject) and now we will change some styles and working with Bindings and Templates. 
 
And for this we will create a project that list some Attributes of a Sample ViewModel and Customizing the style.

## Lets start with Model and ViewModel of the Sample.

Start creating two folders in the shared project, to keep the organization. 
The first One is *Model* and the second *ViewModel*.

On the *Model* Folder create a new Class file named *ModelSample* and change the content as bellow.
The Model created has 4 basic attributes that will allow future examples and validations.

```csharp
namespace MySampleProject.Model;

public class ModelSample
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool Active { get; set; }
}
```

On the *ViewModel* Folder create a new Class file named *ViewModelSample* and change the content as bellow.

```csharp
using MySampleProject.Model;

namespace MySampleProject.ViewModel;

public class ViewModelSample
{
	public string DirectAttribute { get; set; }
	public List<ModelSample> SampleList { get; set; }

	public ViewModelSample()
    {
		DirectAttribute = "AttributeName";
		SampleList = LoadSampleData();
    }

    private List<ModelSample> LoadSampleData()
    {
        List<ModelSample> data = new List<ModelSample>
        {
            new ModelSample { Name = "Sample 1", Description = "Description 1", Active = true },
            new ModelSample { Name = "Sample 2", Description = "Description 2", Active = true },
            new ModelSample { Name = "Sample 3", Description = "Description 3", Active = false }
        };

        return data;
    }
}
```

The above code creates a model and a viewmodel that allow you to list a sequence of sample data.
In the ViewModel, the Model's data is already loaded into the Instance of the class itself.



## Custom your onw C# Markup project

Chage the *MainPage.cs* to have a different content as the sample bellow.

```csharp

using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using MySampleProject.Model;
using MySampleProject.ViewModel;
using System;

namespace MySampleProject;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		//Create an ViewModel that load a list of samples
		DataContext = new ViewModelSample();

		//Set a ViewModel to the DataContext
		this.DataContext<ViewModelSample>((page, vm) => page
			.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))

			//Set Resources using C# Markup
			.Resources
			(
				r => r
					.Add("Icon_Create", "F1 M 0 10.689374307777618 L 0 13.501874923706055 L 2.8125 13.501874923706055 L 11.107499599456787 5.206874544314932 L 8.295000314712524 2.394375001270064 L 0 10.689374307777618 Z M 13.282499313354492 3.03187490723031 C 13.574999302625656 2.7393749282891826 13.574999302625656 2.266875037959408 13.282499313354492 1.97437505901828 L 11.527500629425049 0.21937498420584567 C 11.235000640153885 -0.07312499473528189 10.762499302625656 -0.07312499473528189 10.469999313354492 0.21937498420584567 L 9.097500085830688 1.591874900865419 L 11.909999370574951 4.404374801538142 L 13.282499313354492 3.03187490723031 L 13.282499313354492 3.03187490723031 Z")
					.Add("Icon_Delete", "F1 M 0.75 12 C 0.75 12.825000017881393 1.4249999821186066 13.5 2.25 13.5 L 8.25 13.5 C 9.075000017881393 13.5 9.75 12.825000017881393 9.75 12 L 9.75 3 L 0.75 3 L 0.75 12 Z M 10.5 0.75 L 7.875 0.75 L 7.125 0 L 3.375 0 L 2.625 0.75 L 0 0.75 L 0 2.25 L 10.5 2.25 L 10.5 0.75 Z")
			)
			.Content(
					new Grid()
						//Set the style using a function to allow us to reuse it in multiple places
						.Style(GetGridStyle())
						.RowDefinitions<Grid>("Auto, *")
						.ColumnDefinitions<Grid>("2*, Auto, 2*")
						.Background(new SolidColorBrush(Colors.Silver))
						.Margin(50)
						.Children(
							new TextBlock()
								.Text("Welcome!!")
								.Padding(50)
								.Grid(row: 0, column: 0),
							new Image()
								.Source(new BitmapImage(new Uri("https://picsum.photos/366/366")))
								.Stretch(Stretch.UniformToFill)
								.Width(70)
								.Height(70)
								.Grid(row: 0, column: 2)
								.HorizontalAlignment(HorizontalAlignment.Right),

							new Grid()
								.Style(GetGridStyle())
								.ColumnDefinitions<Grid>("*, *, *")
								.Background(new SolidColorBrush(Color.FromArgb(255, 233, 233, 233)))
								.Grid(grid => grid.Row(1).ColumnSpan(3))
								.Children(

									new StackPanel()
										.Orientation(Orientation.Vertical)
										.Grid(grid => grid.Column(0))
										.Children(

											//C# Markup easily allows code reuse, as shown below.
											GetReusedCodeForTitle("Basics Bindings", "Direct Attribute"),

											new TextBlock()
												.Margin(0,20,0,0)
												.Text("Bind")
												.FontSize(14)
												.FontWeight(FontWeights.Bold),
											
											//Setting some Binding 
											new TextBlock()
												.Text(x => x.Bind(() => vm.DirectAttribute)),

											new TextBlock()
												.Margin(0, 20, 0, 0)
												.Text("Short Bind")
												.FontSize(14)
												.FontWeight(FontWeights.Bold),

											//Setting some Binding using the shorthand version
											new TextBlock()
												.Text(() => vm.DirectAttribute),

											new TextBlock()
												.Margin(0, 20, 0, 0)
												.Text("Named Bind")
												.FontSize(14)
												.FontWeight(FontWeights.Bold),

											//Setting some Binding using a Path string
											new TextBlock()
												.Text(x => x.Bind("DirectAttribute"))

										),
									new StackPanel()
										.Grid(grid => grid.Column(1))
										.Orientation(Orientation.Vertical)
										.Children(
											GetReusedCodeForTitle("GridView", "Using Binding"),

											//Using GridView to display information using Binding the ItemsSource and using ItemTemplate.
											//Notice how simple it is to use templates in C# Markup.
											new GridView()
												.Grid(row: 1, columnSpan: 2)//Attached Properties
												.ItemsSource(x => x.Bind(() => vm.SampleList))
												.ItemTemplate<ModelSample>((sample) => GetDataTemplate())

										),
									new StackPanel()
										.Grid(grid => grid.Column(2))
										.Orientation(Orientation.Vertical)
										.Children(

											GetReusedCodeForTitle("ListView", "Using ItemTemplateSelector"),

											//Using ListView to display information using ItemTemplateSelector to customize the layout.
											new ListView()
												.ItemsSource(x => x.Bind(() => vm.SampleList))
												.ItemTemplateSelector<ModelSample>((item, selector) => selector
													.Case(v => v.Active, () => GetDataTemplateActive())
													.Case(v => !v.Active, () => GetDataTemplateInative())
													.Default(() => new TextBlock().Text("Some Sample"))
												)
										)
								)
						)
			)
		);
	}

	public StackPanel GetReusedCodeForTitle(string Title, string SubTitle)
	{
		return new StackPanel().Children(
				new TextBlock()
					.Text(Title)
					.FontSize(18)
					.FontWeight(FontWeights.Bold),
				new TextBlock()
					.Text(SubTitle)
					.FontSize(16)
					.FontWeight(FontWeights.Bold)
			);
	}


	//Using Style
	public Style GetGridStyle()
	{
		return new Style<Grid>()
			.Setters(s => s.Padding(50))
			.Setters(s => s.BorderBrush(new SolidColorBrush(Colors.Blue)))
			.Setters(s => s.BorderThickness(1))
			.Setters(s => s.CornerRadius(30));
	}

	//Using Template
	public StackPanel GetDataTemplate()
	{
		return new StackPanel().Orientation(Orientation.Horizontal).Children(
				new TextBlock().Margin(10).Text(x => x.Bind("Name")),
				new TextBlock().Margin(10).Text(x => x.Bind("Description")),
				new TextBlock().Margin(10).Text(x => x.Bind("Active"))
			);
	}

	//Using Template
	public StackPanel GetDataTemplateInative()
	{
		return new StackPanel().Orientation(Orientation.Horizontal).Children(
				new TextBlock().Margin(10).Text(x => x.Bind("Name")),
				new TextBlock().Margin(10).Text(x => x.Bind("Description")),
				new Button()
					.Width(40)
					.Height(40)
					//sample of the usage of some StaticResource
					.Content(new PathIcon().Data(StaticResource.Get<Geometry>("Icon_Create")))
			);
	}

	public StackPanel GetDataTemplateActive()
	{
		return new StackPanel().Orientation(Orientation.Horizontal).Children(
				new TextBlock().Margin(10).Text(x => x.Bind("Name")),
				new TextBlock().Margin(10).Text(x => x.Bind("Description")),
				new Button()
					.Width(40)
					.Height(40)
					.Content(new PathIcon().Data(StaticResource.Get<Geometry>("Icon_Delete")))
			);
	}
}

```


## Important points.

### Change Style

You can use this tutorial to learn how to change the style and reuse the style in multiple places.

The usage.
Note that we just set some reusable function to the .Style() and it is done. Simple like that.

```csharp
new Grid()
	//Set the style using a function to allow us to reuse it in multiple places
	.Style(GetGridStyle())
```				

Set up a basic style function. Use the Style Setters to be able to define attributes for the desired element.

```csharp
//Using Style
public Style GetGridStyle()
{
	return new Style<Grid>()
		.Setters(s => s.Padding(50))
		.Setters(s => s.BorderBrush(new SolidColorBrush(Colors.Blue)))
		.Setters(s => s.BorderThickness(1))
		.Setters(s => s.CornerRadius(30));
}

```

> Note that the GetGridStyle function is being used on two Grid elements on the UI.

### Create a new ViewModel and add as a DataContext on the Page.

In this case we are creating a ViewModel for the sole purpose of showing how to include it on the page as a DataContext.
So this ViewModel is already being initialized with the data loaded in the constructor.

```csharp
public MainPage()
{
	//Create an ViewModel that load a list of samples
	DataContext = new ViewModelSample();

	//Set a ViewModel to the DataContext
	this.DataContext<ViewModelSample>((page, vm) => page
					.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))

```	

### Code reuse

C# Markup allows code reuse with great ease.
The example below shows how the GetReusedCodeForTitle function was created to show the Title and Subtitle.

```csharp
new StackPanel()
.Orientation(Orientation.Vertical)
.Grid(grid => grid.Column(0))
.Children(

```

Set up a basic, reusable function.

```csharp
//C# Markup easily allows code reuse, as shown below.
GetReusedCodeForTitle("Basics Bindings", "Direct Attribute"),

```

> Note that the Function is used 3 times in the code.


### Define binding using different usages

C# markup allows you to define bindings using various shapes, which makes it easier to use and speeds up coding.
The example below shows how the GetReusedCodeForTitle function was created to show the Title and Subtitle.

The code below shows 3 different ways to use it.

The regular Binding. Using *x => x.Bind(() =>*.
```csharp
											
//Setting some Binding 
new TextBlock().Text(x => x.Bind(() => vm.DirectAttribute)),

```	

The reduced Binding. Using *() =>*.

```csharp
//Setting some Binding using the shorthand version
new TextBlock().Text(() => vm.DirectAttribute),

```	

And the Binding passing the Path to the attribute. Using *x => x.Bind("AttibuteName")*.

```csharp

//Setting some Binding using a Path string
new TextBlock().Text(x => x.Bind("DirectAttribute"))

```	

### Using ItemTemplate and ItemTemplateSelector to list information from a ViewModel

In this case, we are going to use the GridView and a ListView to show the list of information that is in the ViewModelSample.SampleList.
And with that we need to set the ItemsSource of the two elements. We use the regular Bind, as explained above.

Using *GridView* and *ItemTemplate*.
In the case of the GridView.ItemTemplate we pass the Model and create a GetDataTemplate function to return the information of the columns that will be listed in the GridView.

```csharp

//Using GridView to display information using Binding the ItemsSource and using ItemTemplate.
//Notice how simple it is to use templates in C# Markup.
new GridView()
	.Grid(row: 1, columnSpan: 2)//Attached Properties
	.ItemsSource(x => x.Bind(() => vm.SampleList))
	.ItemTemplate<ModelSample>((sample) => GetDataTemplate())

```

Using ListView and ItemTemplateSelector.

For the ListView.ItemTemplateSelector, we created a simple conditional just to show how a TemplateSelector works in practice. 
And the conditional calls two different functions so the cases are handled differently.

```csharp

//Using ListView to display information using ItemTemplateSelector to customize the layout.
new ListView()
	.ItemsSource(x => x.Bind(() => vm.SampleList))
	.ItemTemplateSelector<ModelSample>((item, selector) => selector
		.Case(v => v.Active, () => GetDataTemplateActive())
		.Case(v => !v.Active, () => GetDataTemplateInative())
		.Default(() => new TextBlock().Text("Some Sample"))
	)
```	

Naturally, you can unify the functions and handle the conditional internally, or even create completely different layouts for each case. 

> Try to do this as a learning exercise.


### Using Resources

C# Markup make easy the use of StaticResource and we use some Geometry Data.
Note that we load the resource on the Page, as show below.
And than on the function GetDataTemplateInative we set the Geometry as a parameter to the PathIcon.Data().
And it is just that. Load some Resources and make use of it.

Set the reference.

```csharp

//Set Resources using C# Markup
.Resources
(
	r => r
		.Add("Icon_Create", "F1 M 0 10.689374307777618 L 0 13.501874923706055 L 2.8125 13.501874923706055 L 11.107499599456787 5.206874544314932 L 8.295000314712524 2.394375001270064 L 0 10.689374307777618 Z M 13.282499313354492 3.03187490723031 C 13.574999302625656 2.7393749282891826 13.574999302625656 2.266875037959408 13.282499313354492 1.97437505901828 L 11.527500629425049 0.21937498420584567 C 11.235000640153885 -0.07312499473528189 10.762499302625656 -0.07312499473528189 10.469999313354492 0.21937498420584567 L 9.097500085830688 1.591874900865419 L 11.909999370574951 4.404374801538142 L 13.282499313354492 3.03187490723031 L 13.282499313354492 3.03187490723031 Z")
		.Add("Icon_Delete", "F1 M 0.75 12 C 0.75 12.825000017881393 1.4249999821186066 13.5 2.25 13.5 L 8.25 13.5 C 9.075000017881393 13.5 9.75 12.825000017881393 9.75 12 L 9.75 3 L 0.75 3 L 0.75 12 Z M 10.5 0.75 L 7.875 0.75 L 7.125 0 L 3.375 0 L 2.625 0.75 L 0 0.75 L 0 2.25 L 10.5 2.25 L 10.5 0.75 Z")
)
```

Using the Geometry set on the Resources.

```csharp

//sample of the usage of some StaticResource
.Content(new PathIcon().Data(StaticResource.Get<Geometry>("Icon_Create")))
```	

## Next Steps

Now start to create your own project using C# Markup from Uno Platform.

- [C# Markup documentation](xref:Reference.Markup.GettingStarted)