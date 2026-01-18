---
name: mvux-selection
description: Implement item selection in MVUX lists. Use when tracking selected items in ListView/GridView, implementing single or multi-selection, or reacting to selection changes.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# MVUX Selection

MVUX provides built-in support for tracking and managing selection in lists, automatically synchronizing with selector controls.

## Prerequisites

- Add `MVUX` to `<UnoFeatures>` in your `.csproj`

```xml
<UnoFeatures>MVUX</UnoFeatures>
```

## Basic Single Selection

### Model Setup

```csharp
using Uno.Extensions.Reactive;
using System.Collections.Immutable;

public partial record Person(string FirstName, string LastName);

public partial record PeopleModel(IPeopleService Service)
{
    // Source list feed
    public IListFeed<Person> PeopleFeed => 
        ListFeed.Async(Service.GetPeopleAsync);
    
    // Selection state
    public IState<Person?> SelectedPerson => State<Person?>.Empty(this);
    
    // Connect selection to list
    public IListState<Person> People => 
        PeopleFeed.Selection(SelectedPerson);
}
```

### XAML

```xml
<Grid RowDefinitions="Auto,*">
    <!-- Selected person display -->
    <StackPanel DataContext="{Binding SelectedPerson}"
                Orientation="Horizontal" Spacing="8">
        <TextBlock Text="Selected:" />
        <TextBlock Text="{Binding FirstName}" />
        <TextBlock Text="{Binding LastName}" />
    </StackPanel>
    
    <!-- List with automatic selection sync -->
    <ListView Grid.Row="1" 
              ItemsSource="{Binding People}"
              SelectionMode="Single">
        <ListView.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal" Spacing="8">
                    <TextBlock Text="{Binding FirstName}" />
                    <TextBlock Text="{Binding LastName}" />
                </StackPanel>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</Grid>
```

**Note:** You don't need to bind `SelectedItem` - MVUX handles this automatically.

## Multi-Selection

### Model Setup

```csharp
public partial record PeopleModel(IPeopleService Service)
{
    public IListFeed<Person> PeopleFeed => 
        ListFeed.Async(Service.GetPeopleAsync);
    
    // Multi-selection state
    public IState<IImmutableList<Person>> SelectedPeople => 
        State<IImmutableList<Person>>.Empty(this);
    
    public IListState<Person> People => 
        PeopleFeed.Selection(SelectedPeople);
}
```

### XAML

```xml
<ListView ItemsSource="{Binding People}"
          SelectionMode="Multiple">
    <ListView.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding FirstName}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

### Display Selection Count

```csharp
public IFeed<string> SelectionCount => 
    SelectedPeople.Select(list => 
        $"Selected: {list?.Count ?? 0} people");
```

```xml
<TextBlock Text="{Binding SelectionCount}" />
```

## Derived Feeds from Selection

### Transform Selected Item

```csharp
public partial record PeopleModel(IPeopleService Service)
{
    public IListFeed<Person> PeopleFeed => 
        ListFeed.Async(Service.GetPeopleAsync);
    
    public IState<Person?> SelectedPerson => State<Person?>.Empty(this);
    
    public IListState<Person> People => 
        PeopleFeed.Selection(SelectedPerson);
    
    // Derived feed from selection
    public IFeed<string> Greeting => 
        SelectedPerson.Select(p =>
            p is null 
                ? "Select a person" 
                : $"Hello, {p.FirstName} {p.LastName}!");
}
```

```xml
<TextBlock Text="{Binding Greeting}" />
```

## React to Selection Changes

### Using ForEach

```csharp
public partial record PeopleModel(IPeopleService Service)
{
    public IState<Person?> SelectedPerson => State<Person?>.Empty(this);
    
    public PeopleModel(IPeopleService service)
    {
        Service = service;
        
        // React to selection changes
        SelectedPerson.ForEach(OnSelectionChanged);
    }
    
    private async ValueTask OnSelectionChanged(Person? person, CancellationToken ct)
    {
        if (person is not null)
        {
            // Load details, log selection, navigate, etc.
            await Service.LogSelectionAsync(person.Id, ct);
        }
    }
}
```

## Use Selection in Commands

MVUX can inject selection into commands automatically:

```csharp
public partial record PeopleModel(IPeopleService Service)
{
    public IListFeed<Person> PeopleFeed => 
        ListFeed.Async(Service.GetPeopleAsync);
    
    public IState<Person?> SelectedPerson => State<Person?>.Empty(this);
    
    public IListState<Person> People => 
        PeopleFeed.Selection(SelectedPerson);
    
    // 'selectedPerson' matches state name - auto-injected
    public async ValueTask EditPerson(Person? selectedPerson, CancellationToken ct)
    {
        if (selectedPerson is not null)
        {
            await Service.EditPersonAsync(selectedPerson, ct);
        }
    }
}
```

```xml
<Button Content="Edit Selected" Command="{Binding EditPerson}" />
```

## Programmatic Selection

### Select Item

```csharp
public async ValueTask SelectFirstPerson(CancellationToken ct)
{
    var people = await PeopleFeed;
    if (people?.Count > 0)
    {
        bool success = await People.TrySelectAsync(people[0], ct);
    }
}
```

### Select Multiple Items

```csharp
public async ValueTask SelectAll(CancellationToken ct)
{
    var people = await PeopleFeed;
    if (people is not null)
    {
        await People.TrySelectAsync(people, ct);
    }
}
```

### Clear Selection

```csharp
public async ValueTask ClearSelection(CancellationToken ct)
{
    await People.ClearSelection(ct);
}
```

## Selection with Key Projection

For complex scenarios where selection is stored as a key:

```csharp
public partial record PersonFilter(string? SelectedPersonId);

public partial record PeopleModel(IPeopleService Service)
{
    public IListFeed<Person> PeopleFeed => 
        ListFeed.Async(Service.GetPeopleAsync);
    
    // Filter state holds selected person's ID
    public IState<PersonFilter> Filter => 
        State.Value(this, () => new PersonFilter(null));
    
    // Selection projected to/from Filter.SelectedPersonId
    public IListState<Person> People => 
        PeopleFeed.Selection<Person, string, PersonFilter>(
            Filter,
            filter => filter.SelectedPersonId);
}

// Person must implement IKeyed<string>
public partial record Person(
    [property: Key] string Id, 
    string FirstName, 
    string LastName) : IKeyed<string>;
```

## Complete Example

### Model

```csharp
public partial record Person(string Id, string FirstName, string LastName);

public partial record PersonDetailsModel(IPeopleService Service)
{
    public IListFeed<Person> PeopleFeed => 
        ListFeed.Async(Service.GetPeopleAsync);
    
    public IState<Person?> SelectedPerson => State<Person?>.Empty(this);
    
    public IListState<Person> People => 
        PeopleFeed.Selection(SelectedPerson);
    
    // Show details for selected person
    public IFeed<PersonDetails?> Details => 
        SelectedPerson.SelectAsync(async (person, ct) =>
            person is null 
                ? null 
                : await Service.GetDetailsAsync(person.Id, ct));
    
    public async ValueTask DeleteSelected(CancellationToken ct)
    {
        var person = await SelectedPerson;
        if (person is not null)
        {
            await Service.DeleteAsync(person.Id, ct);
            await People.RemoveAllAsync(p => p.Id == person.Id, ct);
        }
    }
}
```

### XAML

```xml
<Page x:Class="MyApp.PeoplePage"
      xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    
    <Grid ColumnDefinitions="*,*">
        <!-- People list -->
        <ListView ItemsSource="{Binding People}"
                  SelectionMode="Single">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding FirstName}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
        
        <!-- Selected person details -->
        <mvux:FeedView Grid.Column="1" Source="{Binding Details}">
            <DataTemplate>
                <StackPanel Spacing="8" Padding="16">
                    <TextBlock Text="{Binding Data.FullName}" 
                               Style="{StaticResource TitleTextBlockStyle}" />
                    <TextBlock Text="{Binding Data.Email}" />
                    <TextBlock Text="{Binding Data.Phone}" />
                    <Button Content="Delete" 
                            Command="{Binding Parent.DeleteSelected}" />
                </StackPanel>
            </DataTemplate>
            
            <mvux:FeedView.NoneTemplate>
                <DataTemplate>
                    <TextBlock Text="Select a person to view details" 
                               HorizontalAlignment="Center" />
                </DataTemplate>
            </mvux:FeedView.NoneTemplate>
        </mvux:FeedView>
    </Grid>
</Page>
```

## Best Practices

1. **Use Selection() operator** to connect list feed to selection state
2. **Use IState<T?>** for single selection (nullable)
3. **Use IState<IImmutableList<T>>** for multi-selection
4. **Use derived feeds** to transform/project selected item
5. **Use ForEach** to run side effects on selection change
6. **Match command parameter names** to selection state for auto-injection
7. **Don't bind SelectedItem** - MVUX handles it automatically
