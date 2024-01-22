---
uid: Uno.Extensions.Markup.HowToUseDataTemplates
---

# How to use Data Templates on Markup

In this tutorial, you will learn how to use Data Templates on C# Markup.

## Creating the ViewModel

Let's create the ViewModel for this page, it will have a `SearchText` property that will be used to filter the results, and a `SearchResults` property that will be used to display the results on the `ObservableCollection<string>`.

> In this sample the CommunityToolkit.MVVM is used.

```cs
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    string SearchText = string.Empty;

    public ObservableCollection<string> SearchResults { get; } = new();

    [RelayCommand]
    public async Task Search()
    {
        SearchResults.Clear();
        var results = await FilterService.Current.GetResults(SearchText);

        foreach(var result in results)
            SearchResults.Add(result);
    }
}
```

## Creating the Page

The Page for this tutorial will be very simple, we will have a `TextBox` at the top that will behave like a search bar, and a `ListView` below that will display the results of the search.

```cs
public sealed partial MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MainViewModel>((page, vm) =>
        {
            page
            .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
            .Content
            (
               new Grid()
                    .RowDefinitions("Auto, *, Auto")
                    .Children
                    (
                        new TextBlox()
                            .Margin(5)
                            .Placeholder("Search...")
                            .Text(() => vm.SearchText)
                            .Grid(row: 0),
                        new ListView()
                            .ItemsSource(() => vm.SearchResults)
                            .ItemTemplate<string>(str => new TextBlock().Text(() => str))
                            .Grid(row: 1),
                        new Button()
                            .Content("Search")
                            .Command(() => vm.SearchCommand)
                            .Grid(row: 2)
                    )
            )
            .Padding(58);
        });
    }
}
```

Let's take a look at the `ItemTemplate` usage, and other ways to use it. On the snippet above, we are using the generic to strongly type our model and be able to use it in a safe way, in this case is just a `string` that will be used on the `TextBlock` control.

On the snippet below you can see other ways that you can use the `ItemTemplate` extension method.

```cs
new ListView()
    .ItemsSource(() => vm.SearchResults)
    .ItemTemplate(() => new TextBlock().Text(x => x.Bind()))
```

As you can see, just the `.Bind()` method is used to bind the current item to the `Text` property of the `TextBlock` control.

And with that we have a simple page that will search for results and display them on a `ListView`, using MVVM.
