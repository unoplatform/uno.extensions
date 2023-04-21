---
uid: Overview.Mvux.Overview
---

# Advanced MVUX topics

This page covers the following topics:

- [Selection](#selection)
- [Pagination](#pagination)
- [Commands](#commands)
- [Observables](#observables)

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

*************

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
The parameter of `AsyncPaginated`, is a delegate taking in a [`PageRequest`](#the-pagerequest-struct) value and a `CancellationToken` and returning an `IListFeed<T>` where `T` is `Person` in our case. This delegate is invoked when a page request comes in.

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

### Manual loading

### Manual loading with cursor

### The `PageRequest` struct

![](../Assets/PageRequest.jpg)

## Commands

*************

## Observables

*************
