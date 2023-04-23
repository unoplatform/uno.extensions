---
uid: Overview.Mvux.Overview
---

# Advanced MVUX topics

This page covers the following topics:

- [Selection](#selection)
  - [Single item selection](#single-item-selection)
  - [Multi-item selection](#multi-item-selection)
- [Pagination](#pagination)
  - [Incremental loading](#incremental-loading)
  - [Offset pagination](#offset-pagination)
  - [Keyset pagination with a cursor](#keyset-pagination-with-a-cursor)
- [Commands](#commands)
- [Inspecting the generated code](#inspecting-the-generated-code)

## Selection

MVUX has embedded support of selection.  
Any control that inherits [`Selector`](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.primitives.selector) (e.g. [`ListView`](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.listview), [`GridView`](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.gridview), [`ComboBox`](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.combobox), [`FlipView`](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.flipview)), has automatic support for updating a List-State with its current selection.  
Binding to the `SelectedItem` property is not even required, as this works automatically.  

To synchronize to the selected value in the `Model` side, use the `Selection` operator of the List-View.

## Recap of the *PeopleApp* example

We'll be using the *PeopleApp* example which we've built step-by-step in [this tutorial](xref:Overview.Mvux.HowToListFeed).  

The *PeopleApp* uses an `IListFeed<T>` where `T` is a `Person` [record](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/record) with the properties `FirstName` and `LastName`.
It has a service which has the following contract:

```c#
public interface IPeopleService
{
    ValueTask<IImmutableList<Person>> GetPeopleAsync(CancellationToken ct);
}
```

It is then used by the `PeopleModel` class which requests the service using a List-Feed.

```c#
public partial record PeopleModel(IPeopleService PeopleService)
{
    public IListFeed<Person> People => ListFeed.Async(PeopleService.GetPeopleAsync);
}
```

The data is then displayed on the View using a `ListView`:

```xaml
<Page ...>
    <ListView ItemsSource="{Binding People}">
        <ListView.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal" Spacing="5">
                    <TextBlock Text="{Binding FirstName}"/>
                    <TextBlock Text="{Binding LastName}"/>
                </StackPanel>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</Page>
```

> [!NOTE]  
> The use of the `FeedView` is not necessary in our example, hence the `ListView` has been extracted from it, and its `ItemsSource` property has been directly data-bound to the Feed.

## Implement selection in the *PeopleApp*

MVUX has two extension methods of `IListFeed<T>`, that enables single or multi selection.

> [!NOTE]  
> The source-code for the sample app demonstrated in this section can be found [here](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/AdvancedPeopleApp).

### Single-item selection

A Feed doesn't store any state, so the `People` property won't be able to hold any information, nor the currently selected item.  
To enable storing the selected value in the model, we'll create an `IState<Person>` which will be updated by the `Selection` operator of the `IListFeed<T>` (it's an extension method).

Let's change the `PeopleModel` as follows:

```c#
public partial record PeopleModel(IPeopleService PeopleService)
{
    public IListFeed<Person> People =>
        ListFeed
        .Async(PeopleService.GetPeopleAsync)
        .Selection(SelectedPerson);

    public IState<Person> SelectedPerson => State<Person>.Empty(this);
}
```

The `SelectedPerson` State is initialized with an empty value using `State<Person>.Empty(this)` (we still need a reference to the current instance to enable caching).

> [!NOTE]  
> Read [this](xref:Overview.Mvux.States#other-ways-to-create-feeds) to learn more about States and the `Empty` factory method.

The `Selection` operator was added to the existing `ListFeed.Async(...)` line, it will listen to the `People` List-Feed, and will affect its selection changes onto the `SelectedPerson` State property.

In the View side, wrap the `ListView` element in a `StackPanel`, and insert additional elements to reflect the currently selected value via the `SelectedPerson` State.  
We'll also add a separator (using `Border`) to be able to distinguish them.

The View code shall look like the following:

```xaml
<Page ...>

    <StackPanel>
        <StackPanel DataContext="{Binding SelectedPerson}" Orientation="Horizontal" Spacing="5">
            <TextBlock Text="Selected person:" />
            <TextBlock Text="{Binding FirstName}"/>
            <TextBlock Text="{Binding LastName}"/>
        </StackPanel>

        <Border Height="2" Background="Gray" />

        <ListView ...>
    </StackPanel>
</Page>
```

When running the app, the top section will reflect the item the user selects in the `ListView`:

![](../Assets/Selection.gif)

> [!NOTE]  
> The source-code for the sample app can be found [here](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/AdvancedPeopleApp).

#### Using a property selector

Selection can also be propagated to a new feed using Select

**************************************************************************

### Multi-item selection

The `Selection` operator has another overload which enables selecting multiple items. An `IListState<Person>` is need for multi-selection instead of `IState<Person>` used above.

In the `PeopleModel`, we'll modify the `SelectedPerson` property to look like the following:

```c#
public IState<IImmutableList<Person>> SelectedPeople => State<IImmutableList<Person>>.Empty(this);
```

Then change `.Selection(SelectedPerson)` to `.Selection(SelectedPeople)`.

Head to the View and enable multi-selection in the `ListView` by changing its `SelectionMode` property to `Multiple`.

> [!NOTE]  
> The source-code for the sample app can be found [here](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/SelectionPeopleApp).

## Pagination

There are several ways to paginate data.

### Incremental loading

The easiest and most straight-forward is use the built-in incremental loading functionality that some controls (e.g. `ListView`, `GridView`) offer via the [`ISupportIncrementalLoading`](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.data.isupportincrementalloading) interface the paginated Feed implements.

For the Pagination example we'll also use the *PeopleApp* example used above in [Selection](#selection), but you can find the full code [here](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/PaginationPeopleApp).

#### Service

Let's change the `GetPeopleAsync` method in the *PeopleService.cs* file to support pagination:

```c#
namespace PaginationPeopleApp;

public partial record Person(string FirstName, string LastName);

public interface IPeopleService
{
    ValueTask<IImmutableList<Person>> GetPeopleAsync(uint pageSize, uint pageIndex, CancellationToken ct);

    ValueTask<int> GetPageCount(int pageSize, CancellationToken ct);
}

public class PeopleService : IPeopleService
{
    public async ValueTask<IImmutableList<Person>> GetPeopleAsync(uint pageSize, uint pageIndex, CancellationToken ct)
    {
        // convert to int for use with LINQ
        var (size, index) = ((int)pageSize, (int)pageIndex);

        // fake delay to simulate loading data
        await Task.Delay(TimeSpan.FromSeconds(1), ct);

        // this is where we would asynchronously load actual data from a remote data store
        var people = GetPeople();

        return people
            .Skip(size * index)
            .Take(size)
            .ToImmutableList();
    }

    // Determines how many pages we'll need to display all the data.
    public async ValueTask<int> GetPageCount(int pageSize, CancellationToken ct) =>
        (int)Math.Ceiling(GetPeople().Length / (double)pageSize);

    private Person[] GetPeople() =>
        new Person[]
        {
            new("Liam", "Wilson"),
            new("Emma", "Murphy"),
            new("Noah", "Jones"),
            new("Olivia", "Harris"),
            new("William", "Jackson"),
            /* ... another gazillion names ... */
        }
    }
}
```

#### Model

```c#
using Uno.Extensions.Reactive;

namespace PaginationPeopleApp;

public partial record PeopleModel(IPeopleService PeopleService)
{
    const int PageSize = 20;

    public IListFeed<Person> PeopleAuto =>
        ListFeed.AsyncPaginated(async (PageRequest pageRequest, CancellationToken ct) =>
            await PeopleService.GetPeopleAsync(pageSize: PageSize, pageIndex: pageRequest.Index, ct));
}
```

The `AsyncPaginated` method generates a `ListFeed` that supports pagination.

Whenever the user scrolls down to see additional data and is hitting the end of the collection displayed in a `ListView`, the pagination List-Feed is automatically triggered with a page request.  
The parameter of `AsyncPaginated`, is a delegate taking in a [`PageRequest`](#the-pagerequest-type) value and a `CancellationToken` and returning an `IListFeed<T>` where `T` is `Person` in our case. This delegate is invoked when a page request comes in.

As you can see, we are sending the `Index` property of the incoming `pageRequest` argument to determine what page we're positioned.  
`PageSize` refers to a constant value of `20`, but the `PageRequest` also has a `DesiredSize` property which the `ListView` can set according to its capacity. So essentially you can substitute `pageSize: PageSize` with `pageSize: pageRequest.DesiredSize` which is a nullable `uint`, and is populated by the `ListView`. We used a constant `PageSize` for the sake of this demonstration.

#### View

```xaml
<Page 
    x:Class="PaginationPeopleApp.MainPage"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:PaginationPeopleApp"
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">

    <Page.Resources>
        <DataTemplate x:Key="PersonDataTemplate" x:DataType="local:Person">
            <StackPanel Orientation="Horizontal" Spacing="2">
                <TextBlock Text="{x:Bind FirstName}"/>
                <TextBlock Text="{x:Bind LastName}"/>
            </StackPanel>
        </DataTemplate>
    </Page.Resources>

    <ListView ItemsSource="{Binding PeopleAuto}" ItemTemplate="{StaticResource PersonDataTemplate}"
        DataFetchSize="20" Header="Next page auto-loading when on available capacity or scrolled"/>

</Page>
```

In addition, make sure the `DataContext` of the `Page` is set to the generated bindable model called `BindablePeopleModel` (*MainPage.xaml.cs*):

```c#
public MainPage()
{
    this.InitializeComponent();

    this.DataContext = new BindablePeopleModel(new PeopleService());
}
```

> [!TIP]  
> You can inspect the generated code by either placing the cursor on the word `BindablePeopleModel` and hitting <kbd>F12</kbd>, see other ways to inspect the generated code [here](#inspecting-the-generated-code).

As you can see, there's nothing special in the XAML code as MVUX is taking advantage of the tools already implemented with the `ListView`.

- When the page load, the first 20 items are loaded (after a delay of one second - as simulated in the service).
- If there's available space in the `ListView`, the next batch of 20 items will be requested from the Feed and loaded from the service thereafter.
- When the user scroll down and hits the bottom of the `ListView`, the next 20 items will be requested.
- This behavior will follow as the user scrolls down and chases the service for more data until all items have been loaded.

Here's what the app renders like:

![](../Assets/PaginationIncrementalLoading.gif)

> [!TIP]  
> You can use these members of the `ListView` to control data loading: [`DataFetchSize`](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.listviewbase.datafetchsize), [`IncrementalLoadingThreshold`](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.listviewbase.incrementalloadingthreshold), and [`IncrementalLoadingTrigger`](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.listviewbase.incrementalloadingthreshold).

> [!NOTE]  
> The source-code for the sample app demonstrated in this section can be found [here](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/PaginationPeopleApp).

### Offset pagination

Offset pagination is controlled via a State that stores the current page index in the Model, and the List-Feed depending on it, using [the `Select` operator](xref:Overview.Mvux.Feeds#select-or-selectasync).  
When the user requests a new page, the current page index state is updated, thereby updating the dependent collection List-Feed.

Using the example started in [incremental loading above](#incremental-loading), we'll add another method to the service, which will disclose to the View how many items are there in total. Getting a count of items is more efficient than enumerating all entries. This is necessary to identify the total number of pages we have.

#### Service

Here's the method added to the service (*PeopleService.cs*):

```c#
public async ValueTask<int> GetPageCount(int pageSize, CancellationToken ct) =>
    (int)Math.Ceiling(GetPeople().Length / (double)pageSize);
```

The signature of `GetPageCount` should also be added to the `IPeopleService` interface:

```c#
ValueTask<int> GetPageCount(int pageSize, CancellationToken ct);
```

#### Model

Let's expand the Model with the following:

```c#
public partial record PeopleModel(IPeopleService PeopleService)
{
    public IListFeed<Person> PeopleAuto ...// will not be used in this example

    const int PageSize = 20;
        
    public IFeed<int> PageCount =>
        Feed.Async(async (ct) => await PeopleService.GetPageCount(PageSize, ct));

    public IState<uint> CurrentPage => State.Value(this, () => 1u);

    public IListFeed<Person> PeopleManual =>
        CurrentPage.SelectAsync(async (currentPage, ct) =>
            // currentPage argument as index based
            await PeopleService.GetPeopleAsync(pageSize: PageSize, pageIndex: currentPage - 1, ct))
        .AsListFeed();

    public async ValueTask Move(int directione, CancellationToken ct)
    {
        var currentPage = await CurrentPage;
        var desiredPage = currentPage + direction;

        if (desiredPage < 0 || desiredPage >= await PageCount)
            return;

        await CurrentPage.Set((uint)desiredPage, ct);
    }
}
```

In the code above, whenever `CurrentPage` changes, `PeopleManual` will update its data accordingly.  
MVUX's code generator generates an async command that is bound to from the View, when the user clicks the designated button to move to either previous or next page, the `Move` method will be invoked with the `direction` argument set to either 1 or -1 added to the current page index.

#### View

Replace the `ListView` from the previous example with this one:

```xaml
<ListView Grid.Column="2" x:Name="manual" ItemsSource="{Binding PeopleManual}"
          ItemTemplate="{StaticResource PersonDataTemplate}" Header="Load single page on demand">
    
    <ListView.Footer>
        <StackPanel Orientation="Horizontal" Spacing="3">

            <Button Content="Previous" Command="{Binding Move}" VerticalAlignment="Center">
                <Button.CommandParameter>
                    <x:Int32>-1</x:Int32>
                </Button.CommandParameter>
            </Button>

            <TextBlock Text="{Binding CurrentPage}" VerticalAlignment="Center" />

            <Button Content="Next" Command="{Binding Move}" VerticalAlignment="Center">
                <Button.CommandParameter>
                    <x:Int32>1</x:Int32>
                </Button.CommandParameter>
            </Button>

        </StackPanel>
    </ListView.Footer>
</ListView>
```

Two buttons were added to the `ListView`'s footer which are bound to the `Move` command, and the `TextBlock` in-between them will display the current page via the `CurrentPage` State it's bound to.  
A `CommandParameter` of either *1* or *-1* has been set to Next and Previous buttons respectively.

![](../Assets/PaginationManual-1.jpg)

> [!TIP]  
> The generated `Move` command can be seen in the generated `BindablePeopleModel` as well, follow the instructions [here](#inspecting-the-generated-code).

When running the app, the first page will be loaded and await the users input to navigate to other pages which will be loaded on-demand:

![](../Assets/PaginationManual-2.gif)

#### The `PageRequest` type

The `PageRequest` type contains the following properties and is used by a paginated Feed to pass information about the desired page the user wants to navigate to. It's the first parameter passed into the `PaginatedAsync` method when invoked:

![](../Assets/PageRequest.jpg)

Its properties are:

|Property|Description|
|---|---|
|Index|The index of the page to be loaded.|
|CurrentCount|This is the total number of items currently in the list.|
|DesiredSize|The desired number of items for the current page, if any.<br/><br/>This is the desired number of items that the view requested to load.<br/>It's expected to be null only for the first page.<br/>Be aware that this might change between pages (especially is user resize the window), DO NOT use in `source.Skip(page.Index * page.DesiredSize).Take(page.DesiredSize)`.<br/>Prefer to use the `CurrentCount` property, e.g. `source.Skip(page.CurrentCount).Take(page.DesiredSize)`.|

### Keyset pagination with a cursor

There are several caveats in using `Skip` and `Take` (Offset pagination) with an arbitrary page size multiplied by the page number.

- When we skip data records, the database might still have to process some the skipped records on its way to the desired ones.
- If any updates have been applied to the records preceding the currently displayed page, and then the user moves to the next or previous page, there might be inconsistencies in showing the consequent data, some of the entries might be skipped or shown twice.

An alternative way to paginate data is by using a 

## Commands

## Inspecting the generated code

Viewing the generated code can be achieved in several ways:

1. Placing the cursor on the class name and hitting <kbd>F12</kbd>:

    ![](../Assets/InspectingGeneratedCode-1.gif)

1. Hitting <kbd>Ctrl</kbd>+<kbd>T</kbd> and typing in the Bindable type name:

    ![](../Assets/InspectingGeneratedCode-2.gif)

1. Another way which can also be used to inspect all code generated by MVUX and even other code generators, is by navigating to the project's analyzers.

    1. Expand the shared project's *Dependencies* object
    2. Expand the current target platform (e.g. *net7.0windows10.0...*)
    3. Expand the *Analyzers* sub menu and then *Uno.Extensions.Reactive.Generator*
    4. Under *Uno.Extensions.Reactive.Generator.FeedsGenerator* you'll find the code generated Bindable Models and proxy types.

    ![](../Assets/InspectingGeneratedCode-3.jpg)
