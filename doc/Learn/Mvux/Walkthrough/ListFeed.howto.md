---
uid: Uno.Extensions.Mvux.ListFeed.HowTo
---
# Loading and displaying lists with MVUX

These how-tos show how to **load a collection from a service**, **show it with `FeedView`**, and **filter it with MVUX** using Uno.Extensions.

Requires the `MVUX` UnoFeature.

---

## 1. Load many items from a service

**Outcome:** You have a model that exposes `IListFeed<Person>` which loads asynchronously from a service.

**Why:** MVUX feeds wrap async data + status so the view can react.

```csharp
// PeopleService.cs
using System.Collections.Immutable;

namespace PeopleApp;

public partial record Person(string FirstName, string LastName);

public interface IPeopleService
{
    ValueTask<IImmutableList<Person>> GetPeopleAsync(CancellationToken ct);
}

public class PeopleService : IPeopleService
{
    public async ValueTask<IImmutableList<Person>> GetPeopleAsync(CancellationToken ct)
    {
        // Simulate slow work
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        var people = new[]
        {
            new Person("Master", "Yoda"),
            new Person("Darth", "Vader")
        };

        return people.ToImmutableList();
    }
}
```

```csharp
// PeopleModel.cs
using Uno.Extensions.Reactive;

namespace PeopleApp;

public partial record PeopleModel(IPeopleService PeopleService)
{
    // IListFeed<T> is the MVUX type for collections
    public IListFeed<Person> People => ListFeed.Async(PeopleService.GetPeopleAsync);
}
```

**Notes:**

* `ListFeed.Async(...)` turns your async method into a **list feed**.
* Use **records** for immutable, RAG-friendly sample entities (as the original doc intended).

---

## 2. Show a list feed with FeedView

**Outcome:** XAML page displays the list, shows loading, and has a refresh button.

**Why:** `FeedView` understands MVUX feeds and exposes `Data` + `Refresh`.

```xml
<!-- MainView.xaml -->
<Page
    x:Class="PeopleApp.MainView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">

    <!-- Bind to the People feed -->
    <mvux:FeedView Source="{Binding People}">
        <DataTemplate>
            <ListView ItemsSource="{Binding Data}">
                <ListView.Header>
                    <Button Content="Refresh"
                            Command="{Binding Refresh}" />
                </ListView.Header>

                <ListView.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal" Spacing="5">
                            <TextBlock Text="{Binding FirstName}" />
                            <TextBlock Text="{Binding LastName}" />
                        </StackPanel>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </DataTemplate>
    </mvux:FeedView>
</Page>
```

```csharp
// MainView.xaml.cs
using Microsoft.UI.Xaml.Controls;

namespace PeopleApp;

public sealed partial class MainView : Page
{
    public MainView()
    {
        this.InitializeComponent();

        // In a real app, this comes from DI
        this.DataContext = new PeopleViewModel(new PeopleService());
    }
}
```

**Important MVUX detail:**

* MVUX will generate `PeopleViewModel` from the `PeopleModel` record.
* `FeedView` passes the current feed value to the template as `Data`.
* `Refresh` is automatically surfaced so you don’t have to write a command.

---

## 3. Refresh a list on demand

**Outcome:** User taps a button, feed reloads.

**Why:** MVUX feeds are **stateless**; refreshing is the right pattern.

This is mostly already in the previous example, but here it is as a single, small how-to.

```xml
<mvux:FeedView Source="{Binding People}">
    <DataTemplate>
        <StackPanel>
            <Button Content="Refresh"
                    Command="{Binding Refresh}" />

            <ListView ItemsSource="{Binding Data}">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding FirstName}" />
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </StackPanel>
    </DataTemplate>
</mvux:FeedView>
```

**What happens:**

* While the feed is reloading, `FeedView` shows its loading template (default or custom).
* When the new data arrives, `Data` is updated and the list redraws.

---

## 4. Filter a list by criteria

**Outcome:** You can type in a search box or toggle a filter, and the list reloads from the service with the criteria.

**Why:** MVUX lets you **compose state + feed**: change the state → re-invoke the feed.

### 4.1. Add a criteria type

```csharp
// PersonCriteria.cs
namespace PeopleApp;

public partial record PersonCriteria(string? Term, bool IsDarkSideOnly)
{
    public bool Match(Person person)
    {
        if (Term is { Length: > 0 } term
            && !person.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase)
            && !person.LastName.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IsDarkSideOnly && !person.IsDarkSide)
        {
            return false;
        }

        return true;
    }
}

// Update the person shape to include the Dark Side flag
public partial record Person(string FirstName, string LastName, bool IsDarkSide);
```

### 4.2. Make the service accept criteria

```csharp
// PeopleService.cs
using System.Collections.Immutable;

namespace PeopleApp;

public interface IPeopleService
{
    ValueTask<IImmutableList<Person>> GetPeopleAsync(PersonCriteria criteria, CancellationToken ct);
}

public class PeopleService : IPeopleService
{
    public async ValueTask<IImmutableList<Person>> GetPeopleAsync(PersonCriteria criteria, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        var all = new[]
        {
            new Person("Master", "Yoda",  false),
            new Person("Darth",  "Vader", true)
        };

        return all
            .Where(criteria.Match)
            .ToImmutableList();
    }
}
```

### 4.3. Expose both state and list feed in the model

```csharp
// PeopleModel.cs
using Uno.Extensions.Reactive;

namespace PeopleApp;

public partial record PeopleModel(IPeopleService PeopleService)
{
    // Editable state – MVUX will generate bindable properties
    public IState<PersonCriteria> Criteria =>
        State.Value(this, () => new PersonCriteria(Term: null, IsDarkSideOnly: false));

    // Whenever Criteria changes, call the service again
    public IListFeed<Person> People =>
        Criteria
            .Select(PeopleService.GetPeopleAsync)   // IFeed<IImmutableList<Person>>
            .AsListFeed();                          // -> IListFeed<Person>
}
```

**Key MVUX bit:**
`Criteria.Select(PeopleService.GetPeopleAsync)` means: *“every time Criteria changes, run the service with the new criteria”*.

### 4.4. Bind XAML to edit the criteria

```xml
<Grid
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">

    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>

    <!-- Filters -->
    <StackPanel Grid.Row="0" Spacing="8">
        <TextBox Header="Search"
                 Text="{Binding Criteria.Term, Mode=TwoWay}" />
        <ToggleSwitch Header="Show the dark side only"
                      IsOn="{Binding Criteria.IsDarkSideOnly, Mode=TwoWay}" />
    </StackPanel>

    <!-- Data -->
    <mvux:FeedView Grid.Row="1"
                   Source="{Binding People}">
        <DataTemplate>
            <ListView ItemsSource="{Binding Data}">
                <ListView.Header>
                    <Button Content="Refresh"
                            Command="{Binding Refresh}" />
                </ListView.Header>

                <ListView.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal" Spacing="4">
                            <TextBlock Text="{Binding FirstName}" />
                            <TextBlock Text="{Binding LastName}" />
                        </StackPanel>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </DataTemplate>
    </mvux:FeedView>
</Grid>
```

**Result:** typing or toggling updates `Criteria` → MVUX re-evaluates → list updates.

---

## 5. Await a list feed in code

**Outcome:** You get the list in model code (not just XAML).

**Why:** Sometimes you need the raw data (logging, transformation, side-effect).

```csharp
public async Task LogPeopleAsync()
{
    // 'this' is PeopleModel
    var people = await this.People; // IListFeed<Person> is awaitable
    foreach (var person in people)
    {
        Console.WriteLine($"{person.FirstName} {person.LastName}");
    }
}
```

**Note:** This is from the original doc’s tip: *“An `IListFeed<T>` is awaitable…”*.

---

## 6. When to use feeds vs states

**Outcome:** You pick the right MVUX type.

* **Use `IListFeed<T>`** when:

  * Data is **read-only**.
  * Data is **pulled** from a service.
  * You want **loading / error / refresh** behavior for free.
* **Use `IListState<T>`** when:

  * You want to **edit** the collection.
  * You want to **cache** or **keep** the value.
  * You want **2-way binding** on the collection.

(Original doc says the same: *“Feeds are stateless… MVUX also provides stateful feeds. For that purpose States … come in handy.”*)
