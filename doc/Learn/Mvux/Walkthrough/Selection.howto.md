---
uid: Uno.Extensions.Mvux.Selection.HowTo
---
# How to select items from a list with MVUX

**Goal:** user taps an item in a `ListView`, the ViewModel (MVUX Model) always knows which `Person` is selected.

**When to use:** you already have an `IListFeed<Person>` (ex: from a service) and you want MVUX to keep a single selected item in an `IState<Person>`.

## What you need

* NuGet: `Uno.Extensions.Reactive`
* A model that exposes an `IListFeed<Person>`

```csharp
public interface IPeopleService
{
    ValueTask<IImmutableList<Person>> GetPeopleAsync(CancellationToken ct);
}

public partial record PeopleModel(IPeopleService PeopleService)
{
    public IListFeed<Person> People => ListFeed.Async(PeopleService.GetPeopleAsync);
}
```

## Make the list selectable

Add the `Selection(...)` operator to the list feed and give it a target state.

```csharp
public partial record PeopleModel(IPeopleService PeopleService)
{
    public IListFeed<Person> People =>
        ListFeed
            .Async(PeopleService.GetPeopleAsync)
            .Selection(SelectedPerson); // 👈 connects UI selection to state

    public IState<Person?> SelectedPerson => State<Person?>.Empty(this);
}
```

**Why:** `IListFeed<T>` does **not** store state. The `Selection(...)` operator bridges the Selector control (like `ListView`) and the MVUX `IState<T>`.

## Bind the list in XAML

```xml
<Page ...>
    <ListView ItemsSource="{Binding People}">
        <ListView.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal" Spacing="5">
                    <TextBlock Text="{Binding FirstName}" />
                    <TextBlock Text="{Binding LastName}" />
                </StackPanel>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</Page>
```

You don’t need to bind `SelectedItem`. The `Selection(...)` operator wires it for you through MVUX.

## Show the selected person

```xml
<Page ...>
    <Grid RowDefinitions="Auto,2,*,">
        <!-- selected person -->
        <StackPanel DataContext="{Binding SelectedPerson}"
                    Orientation="Horizontal"
                    Spacing="5">
            <TextBlock Text="Selected person:" />
            <TextBlock Text="{Binding FirstName}" />
            <TextBlock Text="{Binding LastName}" />
        </StackPanel>

        <Border Grid.Row="1" Height="2" Background="Gray" />

        <!-- list -->
        <ListView Grid.Row="2"
                  ItemsSource="{Binding People}" />
    </Grid>
</Page>
```

**Result:** every tap in the `ListView` updates `SelectedPerson` in the model.

---

## React to selection with derived text

**Goal:** build a feed that turns the selected person into a string (ex: “Hello Jane Doe!”) so you can bind directly in XAML.

**When to use:** you already have `SelectedPerson : IState<Person?>`.

## Create a derived feed

```csharp
public IFeed<string> GreetingForSelected =>
    SelectedPerson.Select(p =>
        p is null
            ? string.Empty
            : $"Hello {p.FirstName} {p.LastName}!");
```

* `Select(...)` = transform the current selected item.
* Null-safe: when nothing is selected, return `string.Empty`.

## Bind it in XAML

```xml
<TextBlock Text="{Binding GreetingForSelected}" />
```

**Result:** changes in selection immediately change the greeting text.

---

## Run logic every time selection changes

**Goal:** run async or business logic when the user selects something (logging, navigation, loading details, setting another state).

**When to use:** you don’t just want to “show” the selection, you want to **react** to it.

## Subscribe with `ForEach(...)`

```csharp
public partial record PeopleModel
{
    private readonly IPeopleService _peopleService;

    public PeopleModel(IPeopleService peopleService)
    {
        _peopleService = peopleService;

        // 👇 runs every time SelectedPerson changes
        SelectedPerson.ForEach(SelectionChanged);
    }

    public IState<Person?> SelectedPerson => State<Person?>.Empty(this);

    public IState<string> Greeting => State.Value(this, () => string.Empty);

    public async ValueTask SelectionChanged(Person? person, CancellationToken ct)
    {
        if (person is null)
        {
            await Greeting.Set(string.Empty, ct);
            return;
        }

        await Greeting.Set($"Hello {person.FirstName} {person.LastName}!", ct);
    }
}
```

**Notes:**

* MVUX manages the subscription lifetime with the model.
* This pattern is good for “when user selects X, update Y”.

## Show the computed value

```xml
<TextBlock Text="{Binding Greeting}" />
```

---

## Select multiple people from a ListView

**Goal:** let the user select **many** items in the UI (e.g. multi-select list) and keep all selections in MVUX.

**When to use:** your page needs to act on several selected items (bulk actions, send to list, etc.).

## Change the model to use a list state

```csharp
public partial record PeopleModel(IPeopleService PeopleService)
{
    public IListFeed<Person> People =>
        ListFeed
            .Async(PeopleService.GetPeopleAsync)
            .Selection(SelectedPeople); // 👈 multi-selection

    public IState<IImmutableList<Person>> SelectedPeople =>
        State<IImmutableList<Person>>.Empty(this);
}
```

Key differences from single select:

* Single select: `IState<Person?>`
* Multi select: `IState<IImmutableList<Person>>`

## Enable multi-select in XAML

```xml
<ListView ItemsSource="{Binding People}"
          SelectionMode="Multiple" />
```

**Result:** each change in UI selection updates `SelectedPeople` with the current list.

## Use the selected list

Example: show the count.

```csharp
public IFeed<string> SelectedCount =>
    SelectedPeople.Select(list => $"Selected: {list.Count}");
```

```xml
<TextBlock Text="{Binding SelectedCount}" />
```

---

## Check current selection from a command

**Goal:** call a command (ex: button click) and have MVUX automatically give you the current selection as a parameter.

**When to use:** user selects an item, then clicks a button “Open”, “Edit”, “Send”.

## Model with command

```csharp
public partial record PeopleModel(IPeopleService PeopleService)
{
    public IListFeed<Person> People =>
        ListFeed
            .Async(PeopleService.GetPeopleAsync)
            .Selection(SelectedPerson);

    public IState<Person?> SelectedPerson => State<Person?>.Empty(this);

    // name matches SelectedPerson -> MVUX auto-resolves
    public ValueTask OpenSelected(Person? selectedPerson)
    {
        if (selectedPerson is null)
        {
            // handle missing selection
            return ValueTask.CompletedTask;
        }

        // do something with selectedPerson
        return ValueTask.CompletedTask;
    }
}
```

## XAML button

```xml
<Button Content="Open"
        Command="{Binding OpenSelected}" />
```

**How it works:** because the command parameter name matches the selection feed name, MVUX passes the current selection into the command.

---

## Project selection into another feed

**Goal:** take the current selection and turn it into something else you can bind to (ex: selected person → person details URL, or → view state).

**When to use:** you don’t want to store it manually; you prefer a pure projection.

## Build the projection

```csharp
public IFeed<string> SelectedFullName =>
    SelectedPerson.Select(p =>
        p is null ? "No selection" : $"{p.FirstName} {p.LastName}");
```

## Bind it

```xml
<TextBlock Text="{Binding SelectedFullName}" />
```

---

## Manually control selection (no Selector)

**Goal:** you want to **set** what’s selected yourself (maybe from code, maybe from a detail page), not only reflect what a `ListView` selected.

**When to use:** selection is not coming from a `Selector` control, or you need to override it.

**How MVUX wants you to do it:** use a **List-State** instead of a List-Feed, then use the list-state selection operators.

## Basic idea (model)

```csharp
public partial record PeopleModel(IPeopleService service)
{
    // load as list state so we can change selection ourselves
    public IListState<Person> People => ListState
        .Async(this, service.GetPeopleAsync);

    // later: People.SelectItem(...), People.ToggleItem(...), etc.
}
```

Then follow the **List-State selection operators** doc (same as in the original page).