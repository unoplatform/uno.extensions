---
uid: Uno.Extensions.Markup.HowToCustomMarkupProject
---

# How to custom a C# Markup project

> If you haven't already set up your environment and create the new Markup project, please follow the steps to [Create your own C# Markup](xref:Uno.Extensions.HowToCreateMarkupProject).

In this tutorial, you'll learn how to custom your project using Uno Platform and C# Markup.
And have a comparison between C# Markup and XAML.

In the previous session we learn how to [Create your own C# Markup](xref:Uno.Extensions.HowToCreateMarkupProject) and now we will change some styles and working with Bindings and Templates.

And for this we will create a project that list some Attributes of a Sample ViewModel and Customizing the style.

## Lets start with Model and ViewModel of the Sample

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

## Start Change some basic Style UI with C# Markup

- You can use this tutorial to learn how to change the style and reuse it in many places.

  ### [**C# Markup**](#tab/cs)

  #### C# Markup

  In this example, we can define the style using two syntaxes.

  First, the direct set of the style.

    ```csharp
    new TextBlock()
        .Style(
            new Style<TextBlock>()
                .Setters(e => e.Padding(50))
        )
    ```

  Second, using a reusable function.

    ```csharp
    new Grid()
        .Style(GetGridStyle())
    ```

  Set up a basic style function, similar to the Style using ResourceDictionary in XAML.
  Use the Style Setters to be able to define attributes for the desired element.

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

  Note that we just set some reusable function to the .Style() and it is done.
  Simple like that.

  See how this part of the code would look.

    ```csharp
    new Grid()
        .Style(GetGridStyle())
        .RowDefinitions<Grid>("Auto, *")
        .ColumnDefinitions<Grid>("2*, Auto, 2*")
        .Background(new SolidColorBrush(Colors.Silver))
        .Margin(50)
        .Children(

            new TextBlock()
                .Style(
                    new Style<TextBlock>()
                        .Setters(e => e.Padding(50))
                )
                .Text("Welcome!!")
                .Grid(row: 0, column: 0),

            new Image()
                .Source(new BitmapImage(new Uri("https://picsum.photos/366/366")))
                .Stretch(Stretch.UniformToFill)
                .Width(70)
                .Height(70)
                .Margin(0,0,50,0)
                .Grid(row: 0, column: 2)
                .HorizontalAlignment(HorizontalAlignment.Right),

            new Grid()
                .Style(GetGridStyle())
                .ColumnDefinitions<Grid>("*, *, *")
                .Background(new SolidColorBrush(Color.FromArgb(255, 233, 233, 233)))
                .Grid(grid => grid.Row(1).ColumnSpan(3))
                .Children(

                    new TextBlock()
                        .Margin(0, 20, 0, 0)
                        .Text("Content")
                        .FontSize(14)
                        .FontWeight(FontWeights.Bold)
                )
        )
    ```

  > Note that the *GetGridStyle* function is being used on two Grid elements on the UI.

  ### [**XAML**](#tab/xaml)

  #### XAML

  That same code in XAML would be written this way.
  Compare and see the similarities and differences.

    ```xml
    <Grid
        Background="Silver"
        Margin="50"
        Style="{StaticResource GetGridStyle}">
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
                    Text="Welcome!!"
                    Grid.Row="0"
                    Grid.Column="0">
                <TextBlock.Style>
                    <Style TargetType="TextBlock">
                        <Setter Property="Padding" Value="50" />
                    </Style>
                </TextBlock.Style>
            </TextBlock>

            <Image
                Source="https://picsum.photos/366/366"
                Stretch="UniformToFill"
                Width="70"
                Height="70"
                Margin="0,0,50,0"
                Grid.Row="0"
                Grid.Column="1"
                Grid.ColumnSpan="2"
                HorizontalAlignment="Right"/>

            <Grid
                Background="#FFE9E9E9"
                Grid.Row="1"
                Grid.ColumnSpan="3"
                Style="{StaticResource GetGridStyle}"
                >
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <TextBlock
                    Text="Content"
                    Margin="0,20,0,0"
                    FontSize="14"
                    FontWeight="Bold"/>

            </Grid>
    </Grid>
    ```

  ### [**Full Code**](#tab/code)

  #### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

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
            this
                .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
                .Content(
                        new Grid()
                            .Style(GetGridStyle())
                            .RowDefinitions<Grid>("Auto, *")
                            .ColumnDefinitions<Grid>("2*, Auto, 2*")
                            .Background(new SolidColorBrush(Colors.Silver))
                            .Margin(50)
                            .Children(

                                new TextBlock()
                                    .Style(
                                        new Style<TextBlock>()
                                            .Setters(e => e.Padding(50))
                                    )
                                    .Text("Welcome!!")
                                    .Grid(row: 0, column: 0),

                                new Image()
                                    .Source(new BitmapImage(new Uri("https://picsum.photos/366/366")))
                                    .Stretch(Stretch.UniformToFill)
                                    .Width(70)
                                    .Height(70)
                                    .Margin(0,0,50,0)
                                    .Grid(row: 0, column: 2)
                                    .HorizontalAlignment(HorizontalAlignment.Right),

                                new Grid()
                                    .Style(GetGridStyle())
                                    .ColumnDefinitions<Grid>("*, *, *")
                                    .Background(new SolidColorBrush(Color.FromArgb(255, 233, 233, 233)))
                                    .Grid(grid => grid.Row(1).ColumnSpan(3))
                                    .Children(

                                        new TextBlock()
                                            .Margin(0, 20, 0, 0)
                                            .Text("Content")
                                            .FontSize(14)
                                            .FontWeight(FontWeights.Bold)
                                    )
                            )
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
    }
    ```

## Create a new ViewModel and add as a DataContext on the Page

- In this case we are creating a ViewModel for the sole purpose of showing how to include it on the page as a DataContext.

- So this ViewModel is already being initialized with the data loaded in the constructor.

  ### [**C# Markup**](#tab/cs)

  #### C# Markup

  - In this code we create an ViewModel that load a list of samples and add this ViewModel to the Page.

    ```csharp
    public MainPage()
    {
        //Create an ViewModel that load a list of samples
        DataContext = new ViewModelSample();

        //Set a ViewModel to the DataContext
        this.DataContext<ViewModelSample>((page, vm) => page
                        .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))

    ```

  ### [**XAML**](#tab/xaml)

  #### XAML

  - In the XAML case, we need to edit the MainPage.xaml.cs and create the new ViewModel and add to the DataContext of the page.

    ```csharp
    public MainPage()
    {
        DataContext = new ViewModelSample();
        this.InitializeComponent();
    }
    ```

  ### [**Full Code**](#tab/code)

  #### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

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
                .Content(
                        new Grid()
                            .Style(GetGridStyle())
                            .RowDefinitions<Grid>("Auto, *")
                            .ColumnDefinitions<Grid>("2*, Auto, 2*")
                            .Background(new SolidColorBrush(Colors.Silver))
                            .Margin(50)
                            .Children(

                                new TextBlock()
                                    .Style(
                                        new Style<TextBlock>()
                                            .Setters(e => e.Padding(50))
                                    )
                                    .Text("Welcome!!")
                                    .Grid(row: 0, column: 0),

                                new Image()
                                    .Source(new BitmapImage(new Uri("https://picsum.photos/366/366")))
                                    .Stretch(Stretch.UniformToFill)
                                    .Width(70)
                                    .Height(70)
                                    .Margin(0,0,50,0)
                                    .Grid(row: 0, column: 2)
                                    .HorizontalAlignment(HorizontalAlignment.Right),

                                new Grid()
                                    .Style(GetGridStyle())
                                    .ColumnDefinitions<Grid>("*, *, *")
                                    .Background(new SolidColorBrush(Color.FromArgb(255, 233, 233, 233)))
                                    .Grid(grid => grid.Row(1).ColumnSpan(3))
                                    .Children(

                                        new TextBlock()
                                            .Margin(0, 20, 0, 0)
                                            .Text("Content")
                                            .FontSize(14)
                                            .FontWeight(FontWeights.Bold)
                                    )
                            )
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
    }
    ```

## Define binding using different usages

- C# markup allows you to define bindings using various shapes, which makes it easier to use and speeds up coding.

- The example below shows how to set the attribute called DirectAttribute.

  ### [**C# Markup**](#tab/cs)

  #### C# Markup

  The code below shows 3 different ways to use it using C# Markup.

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

  ### [**XAML**](#tab/xaml)

  #### XAML

  The code below shows 3 different ways to use it using XAML.

  The regular Binding.

    ```xml

    //Setting some Binding
    <TextBlock
            Text="{Binding DirectAttribute}"/>
    ```

  The reduced Bind.

    ```xml
    //Setting some Binding using the x:Bind version
    <TextBlock
            Text="{x:Bind vm.DirectAttribute}"/>
    ```

  And the Binding passing the Path to the attribute..

    ```xml
    //Setting some Binding using a Path string
    <TextBlock
            Text="{Binding Path=DirectAttribute}"/>
    ```

  ### [**Full Code**](#tab/code)

  #### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

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
                .Content(
                        new Grid()
                            .Style(GetGridStyle())
                            .RowDefinitions<Grid>("Auto, *")
                            .ColumnDefinitions<Grid>("2*, Auto, 2*")
                            .Background(new SolidColorBrush(Colors.Silver))
                            .Margin(50)
                            .Children(

                                new TextBlock()
                                    .Style(
                                        new Style<TextBlock>()
                                            .Setters(e => e.Padding(50))
                                    )
                                    .Text("Welcome!!")
                                    .Grid(row: 0, column: 0),

                                new Image()
                                    .Source(new BitmapImage(new Uri("https://picsum.photos/366/366")))
                                    .Stretch(Stretch.UniformToFill)
                                    .Width(70)
                                    .Height(70)
                                    .Margin(0,0,50,0)
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

                                                new TextBlock()
                                                    .Text("Basics Bindings")
                                                    .FontSize(18)
                                                    .FontWeight(FontWeights.Bold),
                                                new TextBlock()
                                                    .Text("Direct Attribute")
                                                    .FontSize(16)
                                                    .FontWeight(FontWeights.Bold),

                                                new TextBlock()
                                                    .Margin(0, 20, 0, 0)
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

                                            )
                                    )
                            )
                ));
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
    }
    ```

## Using ItemTemplate and ItemTemplateSelector to list information from a ViewModel

- In this case, we are going to use the GridView and a ListView to show the list of information that is in the ViewModelSample.SampleList.
- And with that we need to set the ItemsSource of the two elements. We use the regular Bind, as explained above.

  ### [**C# Markup**](#tab/cs)

  #### C# Markup

  ##### Using *GridView* and *ItemTemplate*

  In the case of the GridView.ItemTemplate we pass the Model and create a GetDataTemplate function to return the information of the columns that will be listed in the GridView.

    ```csharp

    //Using GridView to display information using Binding the ItemsSource and using ItemTemplate.
    //Notice how simple it is to use templates in C# Markup.
    new GridView()
        .Grid(row: 1, columnSpan: 2)//Attached Properties
        .ItemsSource(x => x.Bind(() => vm.SampleList))
        .ItemTemplate<ModelSample>((sample) => GetDataTemplate())

    ```

  ##### Using ListView and ItemTemplateSelector

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

  ##### Creating the Templates

  Set up a basic Template function, similar to the DataTemplate using ResourceDictionary in XAML.

    ```csharp

    //Using Template
    public StackPanel GetDataTemplate()
    {
        return new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Children(
                new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                new TextBlock()
                        .Margin(10)
                        .Text(x => x.Bind("Active"))
            );
    }

    //Using Template Inactive
    public StackPanel GetDataTemplateInative()
    {
        return new StackPanel().Orientation(Orientation.Horizontal).Children(
                new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                new Button().Content("Inative")
            );
    }

    //Using Template Active
    public StackPanel GetDataTemplateActive()
    {
        return new StackPanel().Orientation(Orientation.Horizontal).Children(
                new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                new Button().Content("Active")
            );
    }

    ```

  Naturally, you can unify the functions and handle the conditional internally, or even create completely different layouts for each case.

  > Try to do this as a learning exercise.

  ### [**XAML**](#tab/xaml)

  #### XAML

  ##### Using *GridView* and *ItemTemplate*

  In the case of the GridView.ItemTemplate we pass the Model and create a new DataTemplate.

    ```xml
        <GridView ItemsSource="{Binding SampleList}" ItemTemplate="{StaticResource GetDataTemplate}"/>
    ```

  ##### Using ListView and ItemTemplateSelector

  For the ListView.ItemTemplateSelector, we pass the Model and create a new ItemTemplateSelector.

    ```xml
        <ListView ItemsSource="{Binding SampleList}" ItemTemplateSelector="{StaticResource ItemTemplateSelector}"/>
    ```

  ##### Creating the Templates

  In XAML to add some DataTemplate we need to add the ResourceDictionary in to the Page.Resources and than create a DataTemplate.

    ```xml
        <Page.Resources>
            <ResourceDictionary>

                <DataTemplate x:Key="GetDataTemplate">
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="{Binding Name}" Margin="10" />
                        <TextBlock Text="{Binding Description}" Margin="10" />
                        <TextBlock Text="{Binding Active}" Margin="10" />
                    </StackPanel>
                </DataTemplate>

            </ResourceDictionary>
        </Page.Resources>
    ```

  And for the ItemTemplateSelector we need to create a new Class and add ItemTemplateSelector into the ResourceDictionary.

    ```csharp
    namespace MySampleProjectXaml.Model;

    internal class MyItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GetDataTemplateActive { get; set; }
        public DataTemplate GetDataTemplateInative { get; set; }

        protected override DataTemplate SelectTemplateCore(object obj)
        {
            if (obj is ModelSample item)
            {
                if (item.Active)
                {
                    return GetDataTemplateActive;
                }
                else
                {
                    return GetDataTemplateInative;
                }
            }

            return base.SelectTemplateCore(obj);
        }
    }

    ```

  Add Resources on the Page.

    ```xml
        <Page.Resources>
            <ResourceDictionary>

                <model:MyItemTemplateSelector x:Key="ItemTemplateSelector">
                    <model:MyItemTemplateSelector.GetDataTemplateActive>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="{Binding Name}" Margin="10" />
                                <TextBlock Text="{Binding Description}" Margin="10" />
                                <Button Content="Active" Margin="10" />
                            </StackPanel>
                        </DataTemplate>
                    </model:MyItemTemplateSelector.GetDataTemplateActive>
                    <model:MyItemTemplateSelector.GetDataTemplateInative>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="{Binding Name}" Margin="10" />
                                <TextBlock Text="{Binding Description}" Margin="10" />
                                <Button Content="Inactive" Margin="10" />
                            </StackPanel>
                        </DataTemplate>
                    </model:MyItemTemplateSelector.GetDataTemplateInative>
                </model:MyItemTemplateSelector>

            </ResourceDictionary>
        </Page.Resources>
    ```

  ### [**Full Code**](#tab/code)

  #### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

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
                .Content(
                        new Grid()
                            .Style(GetGridStyle())
                            .RowDefinitions<Grid>("Auto, *")
                            .ColumnDefinitions<Grid>("2*, Auto, 2*")
                            .Background(new SolidColorBrush(Colors.Silver))
                            .Margin(50)
                            .Children(

                                new TextBlock()
                                    .Style(
                                        new Style<TextBlock>()
                                            .Setters(e => e.Padding(50))
                                    )
                                    .Text("Welcome!!")
                                    .Grid(row: 0, column: 0),

                                new Image()
                                    .Source(new BitmapImage(new Uri("https://picsum.photos/366/366")))
                                    .Stretch(Stretch.UniformToFill)
                                    .Width(70)
                                    .Height(70)
                                    .Margin(0,0,50,0)
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

                                                new TextBlock()
                                                    .Text("Basics Bindings")
                                                    .FontSize(18)
                                                    .FontWeight(FontWeights.Bold),
                                                new TextBlock()
                                                    .Text("Direct Attribute")
                                                    .FontSize(16)
                                                    .FontWeight(FontWeights.Bold),

                                                new TextBlock()
                                                    .Margin(0, 20, 0, 0)
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

                                                new TextBlock()
                                                    .Text("GridView")
                                                    .FontSize(18)
                                                    .FontWeight(FontWeights.Bold),
                                                new TextBlock()
                                                    .Text("Using Binding")
                                                    .FontSize(16)
                                                    .FontWeight(FontWeights.Bold),

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

                                                new TextBlock()
                                                    .Text("ListView")
                                                    .FontSize(18)
                                                    .FontWeight(FontWeights.Bold),
                                                new TextBlock()
                                                    .Text("Using ItemTemplateSelector")
                                                    .FontSize(16)
                                                    .FontWeight(FontWeights.Bold),

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
                ));
        }

        //Using Template
        public StackPanel GetDataTemplate()
        {
            return new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new TextBlock()
                            .Margin(10)
                            .Text(x => x.Bind("Active"))
                );
        }

        //Using Template
        public StackPanel GetDataTemplateInative()
        {
            return new StackPanel().Orientation(Orientation.Horizontal).Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new Button()
                        .Content("Inative")
                );
        }

        public StackPanel GetDataTemplateActive()
        {
            return new StackPanel().Orientation(Orientation.Horizontal).Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new Button()
                        .Content("Active")
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
    }
    ```

### Code reuse

- C# Markup allows code reuse with great easy.
- The example below shows how the GetReusedCodeForTitle function was created to show the Title and Subtitle.

  #### [**C# Markup**](#tab/cs)

  ##### C# Markup

  Notice that there are parts of the code that are repeated with few changes.
  For example the codes below.

    ```csharp

    new TextBlock()
        .Text("Basics Bindings")
        .FontSize(18)
        .FontWeight(FontWeights.Bold),
    new TextBlock()
        .Text("Direct Attribute")
        .FontSize(16)
        .FontWeight(FontWeights.Bold),

    ```

  Thus, we can unify these codes in a single function and reuse them in different parts of the code.

  Set up a basic, reusable function.

    ```csharp

    public StackPanel GetReusedCodeForTitle(string Title, string SubTitle)
    {
        return new StackPanel().Children(
                new TextBlock()
                    .Text(Title)
                    .FontSize(18)
                    .FontWeight(FontWeights.Bold)
                    .Name(out var searchText),
                new TextBlock()
                    .Text(SubTitle)
                    .FontSize(16)
                    .FontWeight(FontWeights.Bold)
            );
    }
    ```

  Than just call the function in several places.

    ```csharp
            //C# Markup easily allows code reuse, as shown below.
            GetReusedCodeForTitle("Basics Bindings", "Direct Attribute"),
    ```

  > Note that the Function can be used 3 times in the code.

  #### [**XAML**](#tab/xaml)

  ##### XAML

  To have a similar result in XAML, we can use a UserControl to allow us to include reusable code on the main page.
  For this we need to create a new UserControl, named UserControlSample in this sample.

  With the following XAML code

    ```xml
        <UserControl
            x:Class="MySampleProjectXaml.UserControlSample"
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:local="using:MySampleProjectXaml"
            xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
            xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
            mc:Ignorable="d"
            d:DesignHeight="300"
            d:DesignWidth="400">

            <StackPanel>
                <TextBlock
                            Text="{Binding Title}"
                            FontSize="18"
                            FontWeight="Bold"/>
                <TextBlock
                            Text="{Binding SubTitle}"
                            FontSize="16"
                            FontWeight="Bold"/>
            </StackPanel>
        </UserControl>

    ```

  And a code behind to allow the passing of parameters.

    ```csharp
        namespace MySampleProjectXaml;

        public sealed partial class UserControlSample : UserControl
        {
            public static readonly DependencyProperty TitleProperty =
                DependencyProperty.Register("Title", typeof(string), typeof(UserControlSample), new PropertyMetadata(string.Empty));

            public string Title
            {
                get { return (string)GetValue(TitleProperty); }
                set { SetValue(TitleProperty, value); }
            }
            public static readonly DependencyProperty SubTitleProperty =
                DependencyProperty.Register("SubTitle", typeof(string), typeof(UserControlSample), new PropertyMetadata(string.Empty));

            public string SubTitle
            {
                get { return (string)GetValue(SubTitleProperty); }
                set { SetValue(SubTitleProperty, value); }
            }

            public UserControlSample()
            {
                InitializeComponent();
                DataContext = this;
            }
        }

    ```

  Than just call the function in several places.

    ```xml
    <local:UserControlSample Title="Basics Bindings" SubTitle="Direct Attribute"/>
    ```

  > Note that the XAML UserContol can be used 3 times in the code as we just do on C# Markup.

  #### [**Full Code**](#tab/code)

  ##### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

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
                .Content(
                        new Grid()
                            .Style(GetGridStyle())
                            .RowDefinitions<Grid>("Auto, *")
                            .ColumnDefinitions<Grid>("2*, Auto, 2*")
                            .Background(new SolidColorBrush(Colors.Silver))
                            .Margin(50)
                            .Children(

                                new TextBlock()
                                    .Style(
                                        new Style<TextBlock>()
                                            .Setters(e => e.Padding(50))
                                    )
                                    .Text("Welcome!!")
                                    .Grid(row: 0, column: 0),

                                new Image()
                                    .Source(new BitmapImage(new Uri("https://picsum.photos/366/366")))
                                    .Stretch(Stretch.UniformToFill)
                                    .Width(70)
                                    .Height(70)
                                    .Margin(0,0,50,0)
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
                                                    .Margin(0, 20, 0, 0)
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
                ));
        }

        //Using Template
        public StackPanel GetDataTemplate()
        {
            return new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new TextBlock()
                            .Margin(10)
                            .Text(x => x.Bind("Active"))
                );
        }

        //Using Template
        public StackPanel GetDataTemplateInative()
        {
            return new StackPanel().Orientation(Orientation.Horizontal).Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new Button()
                        .Content("Inative")
                );
        }

        public StackPanel GetDataTemplateActive()
        {
            return new StackPanel().Orientation(Orientation.Horizontal).Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new Button()
                        .Content("Active")
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

        public StackPanel GetReusedCodeForTitle(string Title, string SubTitle)
        {
            return new StackPanel().Children(
                    new TextBlock()
                        .Text(Title)
                        .FontSize(18)
                        .FontWeight(FontWeights.Bold)
                        .Name(out var searchText),
                    new TextBlock()
                        .Text(SubTitle)
                        .FontSize(16)
                        .FontWeight(FontWeights.Bold)
                );
        }
    }
    ```

### Using Resources

- C# Markup make easy the use of StaticResource and we use some Geometry Data.

- And than on the function GetDataTemplateInative we set the Geometry as a parameter to the PathIcon.Data().

- And it is just that. Load some Resources and make use of it.

  #### [**C# Markup**](#tab/cs)

  ##### C# Markup

  Note that we load the resource on the Page, as show below.

  ###### Set the reference

    ```csharp

    //Set Resources using C# Markup
    .Resources
    (
        r => r
            .Add("Icon_Create", "F1 M 0 10.689374307777618 L 0 13.501874923706055 L 2.8125 13.501874923706055 L 11.107499599456787 5.206874544314932 L 8.295000314712524 2.394375001270064 L 0 10.689374307777618 Z M 13.282499313354492 3.03187490723031 C 13.574999302625656 2.7393749282891826 13.574999302625656 2.266875037959408 13.282499313354492 1.97437505901828 L 11.527500629425049 0.21937498420584567 C 11.235000640153885 -0.07312499473528189 10.762499302625656 -0.07312499473528189 10.469999313354492 0.21937498420584567 L 9.097500085830688 1.591874900865419 L 11.909999370574951 4.404374801538142 L 13.282499313354492 3.03187490723031 L 13.282499313354492 3.03187490723031 Z")
            .Add("Icon_Delete", "F1 M 0.75 12 C 0.75 12.825000017881393 1.4249999821186066 13.5 2.25 13.5 L 8.25 13.5 C 9.075000017881393 13.5 9.75 12.825000017881393 9.75 12 L 9.75 3 L 0.75 3 L 0.75 12 Z M 10.5 0.75 L 7.875 0.75 L 7.125 0 L 3.375 0 L 2.625 0.75 L 0 0.75 L 0 2.25 L 10.5 2.25 L 10.5 0.75 Z")
    )
    ```

  ###### Using the Geometry set on the Resources

    ```csharp
    //sample of the usage of some StaticResource
    new Button()
        .Content(new PathIcon().Data(StaticResource.Get<Geometry>("Icon_Create")))
    ```

  #### [**XAML**](#tab/xaml)

  ##### XAML

  To have a similar result in XAML, we can use a UserControl to allow us to include reusable code on the main page.
  For this we need to create a new UserControl, named UserControlSample in this sample.

  ###### With the following XAML code to set the reference

    ```xml
    <Page.Resources>
        <ResourceDictionary>
            <x:String x:Key="Icon_Create">F1 M 0 10.689374307777618 L 0 13.501874923706055 L 2.8125 13.501874923706055 L 11.107499599456787 5.206874544314932 L 8.295000314712524 2.394375001270064 L 0 10.689374307777618 Z M 13.282499313354492 3.03187490723031 C 13.574999302625656 2.7393749282891826 13.574999302625656 2.266875037959408 13.282499313354492 1.97437505901828 L 11.527500629425049 0.21937498420584567 C 11.235000640153885 -0.07312499473528189 10.762499302625656 -0.07312499473528189 10.469999313354492 0.21937498420584567 L 9.097500085830688 1.591874900865419 L 11.909999370574951 4.404374801538142 L 13.282499313354492 3.03187490723031 L 13.282499313354492 3.03187490723031 Z</x:String>
            <x:String x:Key="Icon_Delete">F1 M 0.75 12 C 0.75 12.825000017881393 1.4249999821186066 13.5 2.25 13.5 L 8.25 13.5 C 9.075000017881393 13.5 9.75 12.825000017881393 9.75 12 L 9.75 3 L 0.75 3 L 0.75 12 Z M 10.5 0.75 L 7.875 0.75 L 7.125 0 L 3.375 0 L 2.625 0.75 L 0 0.75 L 0 2.25 L 10.5 2.25 L 10.5 0.75 Z</x:String>
        </ResourceDictionary>
    </Page.Resources>
    ```

  ###### Using the String of the Geometry set on the Resources

    ```xml
    <Button>
        <Button.Content>
            <PathIcon Data="{StaticResource Icon_Create}" />
        </Button.Content>
    </Button>
    ```

  #### [**Full Code**](#tab/code)

  ##### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

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
                            .Style(GetGridStyle())
                            .RowDefinitions<Grid>("Auto, *")
                            .ColumnDefinitions<Grid>("2*, Auto, 2*")
                            .Background(new SolidColorBrush(Colors.Silver))
                            .Margin(50)
                            .Children(

                                new TextBlock()
                                    .Style(
                                        new Style<TextBlock>()
                                            .Setters(e => e.Padding(50))
                                    )
                                    .Text("Welcome!!")
                                    .Grid(row: 0, column: 0),

                                new Image()
                                    .Source(new BitmapImage(new Uri("https://picsum.photos/366/366")))
                                    .Stretch(Stretch.UniformToFill)
                                    .Width(70)
                                    .Height(70)
                                    .Margin(0,0,50,0)
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
                                                    .Margin(0, 20, 0, 0)
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
                ));
        }

        //Using Template
        public StackPanel GetDataTemplate()
        {
            return new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new TextBlock()
                            .Margin(10)
                            .Text(x => x.Bind("Active"))
                );
        }

        //Using Template
        public StackPanel GetDataTemplateInative()
        {
            return new StackPanel().Orientation(Orientation.Horizontal).Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new Button()
                        .Content(new PathIcon().Data(StaticResource.Get<Geometry>("Icon_Delete")))
                );
        }

        public StackPanel GetDataTemplateActive()
        {
            return new StackPanel().Orientation(Orientation.Horizontal).Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new Button()
                        .Content(new PathIcon().Data(StaticResource.Get<Geometry>("Icon_Create")))
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

        public StackPanel GetReusedCodeForTitle(string Title, string SubTitle)
        {
            return new StackPanel().Children(
                    new TextBlock()
                        .Text(Title)
                        .FontSize(18)
                        .FontWeight(FontWeights.Bold)
                        .Name(out var searchText),
                    new TextBlock()
                        .Text(SubTitle)
                        .FontSize(16)
                        .FontWeight(FontWeights.Bold)
                );
        }
    }
    ```

## All Code

- Now you can check the full code using C# Markup and XAML.

  ### [**Full XAML Code**](#tab/cli)

  #### Full XAML code

  - Example of the complete code on the MainPage.xaml, so you can follow along in your own project.

    ```xml
    <Page x:Class="MySampleProjectXaml.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:MySampleProjectXaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"
        xmlns:model="using:MySampleProjectXaml.Model">
        <Page.Resources>
            <ResourceDictionary>
                <x:String x:Key="Icon_Create">F1 M 0 10.689374307777618 L 0 13.501874923706055 L 2.8125 13.501874923706055 L 11.107499599456787 5.206874544314932 L 8.295000314712524 2.394375001270064 L 0 10.689374307777618 Z M 13.282499313354492 3.03187490723031 C 13.574999302625656 2.7393749282891826 13.574999302625656 2.266875037959408 13.282499313354492 1.97437505901828 L 11.527500629425049 0.21937498420584567 C 11.235000640153885 -0.07312499473528189 10.762499302625656 -0.07312499473528189 10.469999313354492 0.21937498420584567 L 9.097500085830688 1.591874900865419 L 11.909999370574951 4.404374801538142 L 13.282499313354492 3.03187490723031 L 13.282499313354492 3.03187490723031 Z</x:String>
                <x:String x:Key="Icon_Delete">F1 M 0.75 12 C 0.75 12.825000017881393 1.4249999821186066 13.5 2.25 13.5 L 8.25 13.5 C 9.075000017881393 13.5 9.75 12.825000017881393 9.75 12 L 9.75 3 L 0.75 3 L 0.75 12 Z M 10.5 0.75 L 7.875 0.75 L 7.125 0 L 3.375 0 L 2.625 0.75 L 0 0.75 L 0 2.25 L 10.5 2.25 L 10.5 0.75 Z</x:String>

                <Style x:Key="GetGridStyle" TargetType="Grid">
                    <Setter Property="Padding" Value="50" />
                    <Setter Property="BorderBrush" Value="Blue" />
                    <Setter Property="BorderThickness" Value="1" />
                    <Setter Property="CornerRadius" Value="30" />
                </Style>
                <DataTemplate x:Key="GetDataTemplate">
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="{Binding Name}" Margin="10" />
                        <TextBlock Text="{Binding Description}" Margin="10" />
                        <TextBlock Text="{Binding Active}" Margin="10" />
                    </StackPanel>
                </DataTemplate>

                <model:MyItemTemplateSelector x:Key="ItemTemplateSelector">
                    <model:MyItemTemplateSelector.GetDataTemplateActive>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="{Binding Name}" Margin="10" />
                                <TextBlock Text="{Binding Description}" Margin="10" />
                                <Button>
                                    <Button.Content>
                                        <PathIcon Data="{StaticResource Icon_Create}" />
                                    </Button.Content>
                                </Button>
                            </StackPanel>
                        </DataTemplate>
                    </model:MyItemTemplateSelector.GetDataTemplateActive>
                    <model:MyItemTemplateSelector.GetDataTemplateInative>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <TextBlock Text="{Binding Name}" Margin="10" />
                                <TextBlock Text="{Binding Description}" Margin="10" />
                                <Button>
                                    <Button.Content>
                                        <PathIcon Data="{StaticResource Icon_Delete}" />
                                    </Button.Content>
                                </Button>
                            </StackPanel>
                        </DataTemplate>
                    </model:MyItemTemplateSelector.GetDataTemplateInative>
                </model:MyItemTemplateSelector>

            </ResourceDictionary>
        </Page.Resources>
        <Grid
            Background="Silver"
            Margin="50"
            Style="{StaticResource GetGridStyle}">
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
                        Text="Welcome!!"
                        Grid.Row="0"
                        Grid.Column="0">
                    <TextBlock.Style>
                        <Style TargetType="TextBlock">
                            <Setter Property="Padding" Value="50" />
                        </Style>
                    </TextBlock.Style>
                </TextBlock>

                <Image
                    Source="https://picsum.photos/366/366"
                    Stretch="UniformToFill"
                    Width="70"
                    Height="70"
                    Margin="0,0,50,0"
                    Grid.Row="0"
                    Grid.Column="1"
                    Grid.ColumnSpan="2"
                    HorizontalAlignment="Right"/>

                <Grid
                    Background="#FFE9E9E9"
                    Grid.Row="1"
                    Grid.ColumnSpan="3"
                    Style="{StaticResource GetGridStyle}"
                    >
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                <StackPanel
                    Orientation="Vertical"
                    Grid.Column="0">

                    <local:UserControlSample Title="Basics Bindings" SubTitle="Direct Attribute"/>

                    <TextBlock
                        Text="Bind"
                        FontSize="14"
                        FontWeight="Bold"
                        Margin="0,20,0,0"/>
                    <TextBlock
                        Text="{Binding DirectAttribute}"/>

                    <TextBlock
                        Text="Short Bind"
                        FontSize="14"
                        FontWeight="Bold"
                        Margin="0,20,0,0"/>
                    <TextBlock
                        Text="{x:Bind vm.DirectAttribute}"/>


                    <TextBlock
                        Text="Named Bind"
                        FontSize="14"
                        FontWeight="Bold"
                        Margin="0,20,0,0"/>
                    <TextBlock
                        Text="{Binding Path=DirectAttribute}"/>

                </StackPanel>


                <StackPanel
                    Orientation="Vertical"
                    Grid.Column="1">

                    <local:UserControlSample Title="GridView" SubTitle="Using Binding"/>

                    <GridView ItemsSource="{Binding SampleList}" ItemTemplate="{StaticResource GetDataTemplate}"/>

                </StackPanel>

                <StackPanel
                    Orientation="Vertical"
                    Grid.Column="2">
                    <local:UserControlSample Title="ListView" SubTitle="Using ItemTemplateSelector"/>

                    <ListView ItemsSource="{Binding SampleList}" ItemTemplateSelector="{StaticResource ItemTemplateSelector}"/>

                </StackPanel>
            </Grid>
        </Grid>
    </Page>

    ```

  ### [**Full C# Markup Code**](#tab/code)

  #### Full C# Markup code

  - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

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
                            .Style(GetGridStyle())
                            .RowDefinitions<Grid>("Auto, *")
                            .ColumnDefinitions<Grid>("2*, Auto, 2*")
                            .Background(new SolidColorBrush(Colors.Silver))
                            .Margin(50)
                            .Children(

                                new TextBlock()
                                    .Style(
                                        new Style<TextBlock>()
                                            .Setters(e => e.Padding(50))
                                    )
                                    .Text("Welcome!!")
                                    .Grid(row: 0, column: 0),

                                new Image()
                                    .Source(new BitmapImage(new Uri("https://picsum.photos/366/366")))
                                    .Stretch(Stretch.UniformToFill)
                                    .Width(70)
                                    .Height(70)
                                    .Margin(0,0,50,0)
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
                                                    .Margin(0, 20, 0, 0)
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
                ));
        }

        //Using Template
        public StackPanel GetDataTemplate()
        {
            return new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new TextBlock()
                            .Margin(10)
                            .Text(x => x.Bind("Active"))
                );
        }

        //Using Template
        public StackPanel GetDataTemplateInative()
        {
            return new StackPanel().Orientation(Orientation.Horizontal).Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new Button()
                        .Content(new PathIcon().Data(StaticResource.Get<Geometry>("Icon_Delete")))
                );
        }

        public StackPanel GetDataTemplateActive()
        {
            return new StackPanel().Orientation(Orientation.Horizontal).Children(
                    new TextBlock().Margin(10).Text(x => x.Bind("Name")),
                    new TextBlock().Margin(10).Text(x => x.Bind("Description")),
                    new Button()
                        .Content(new PathIcon().Data(StaticResource.Get<Geometry>("Icon_Create")))
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
        public StackPanel GetReusedCodeForTitle(string Title, string SubTitle)
        {
            return new StackPanel().Children(
                    new TextBlock()
                        .Text(Title)
                        .FontSize(18)
                        .FontWeight(FontWeights.Bold)
                        .Name(out var searchText),
                    new TextBlock()
                        .Text(SubTitle)
                        .FontSize(16)
                        .FontWeight(FontWeights.Bold)
                );
        }
    }

    ```

## Next Steps

- [Custom your own C# Markup - Learn how to change Visual States and User Controls](xref:Uno.Extensions.HowToCustomMarkupProjectVisualStates)
- [Custom your own C# Markup - Learn how to use Toolkit](xref:Uno.Extensions.HowToCustomMarkupProjectToolkit)
- [Custom your own C# Markup - Learn how to Change the Theme](xref:Uno.Extensions.HowToCustomMarkupProjectTheme)
- [Custom your own C# Markup - Learn how to use MVUX](xref:Uno.Extensions.HowToCustomMarkupProjectMVUX)
