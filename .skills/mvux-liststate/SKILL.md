---
name: mvux-liststate
description: Create and use IListState<T> for mutable reactive collections in MVUX. Use when editing collections, adding/removing items, managing selection, or synchronizing lists with two-way binding.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# MVUX ListState

`IListState<T>` is a mutable reactive collection that supports adding, removing, updating items, and managing selection.

## Prerequisites

- Add `MVUX` to `<UnoFeatures>` in your `.csproj`

```xml
<UnoFeatures>MVUX</UnoFeatures>
```

## What is a ListState?

An `IListState<T>` provides:
- Mutable reactive collection
- Add, remove, update operations
- Selection management
- Two-way data binding support

## ListState vs ListFeed

| Aspect | IListFeed<T> | IListState<T> |
|--------|--------------|---------------|
| Mutability | Read-only | Read/Write |
| Operations | Filter only | Add/Remove/Update |
| Selection | Basic | Full management |
| Use Case | Display data | Edit data |

## Creating ListStates

### Empty ListState

```csharp
using Uno.Extensions.Reactive;
using System.Collections.Immutable;

public partial record ShoppingCartModel
{
    public IListState<CartItem> Items => ListState<CartItem>.Empty(this);
}
```

### With Initial Value

```csharp
public partial record FavoritesModel
{
    private readonly IImmutableList<string> _defaults = 
        ImmutableArray.Create("Item1", "Item2", "Item3");
    
    public IListState<string> Favorites => 
        ListState.Value(this, () => _defaults);
}
```

### From Async Source

```csharp
public partial record TodoModel(ITodoService Service)
{
    public IListState<TodoItem> Todos => 
        ListState.Async(this, Service.GetTodosAsync);
}
```

### From AsyncEnumerable

```csharp
public partial record NotificationsModel(INotificationService Service)
{
    public IListState<Notification> Notifications => 
        ListState.AsyncEnumerable(this, Service.GetNotificationStream);
}
```

### From ListFeed

```csharp
public partial record PeopleModel(IPeopleService Service)
{
    private IListFeed<Person> PeopleFeed => 
        ListFeed.Async(Service.GetPeopleAsync);
    
    public IListState<Person> People => 
        ListState.FromFeed(this, PeopleFeed);
}
```

## List Operations

### Add Item

```csharp
public async ValueTask AddItem(CartItem item, CancellationToken ct)
{
    await Items.AddAsync(item, ct);
}
```

### Insert at Beginning

```csharp
public async ValueTask InsertAtTop(CartItem item, CancellationToken ct)
{
    await Items.InsertAsync(item, ct);
}
```

### Remove Items

```csharp
// Remove items matching condition
public async ValueTask RemoveCompleted(CancellationToken ct)
{
    await Todos.RemoveAllAsync(
        match: todo => todo.IsCompleted, 
        ct: ct);
}
```

### Update All Matching Items

```csharp
public async ValueTask MarkAllComplete(CancellationToken ct)
{
    await Todos.UpdateAllAsync(
        match: todo => !todo.IsCompleted,
        updater: todo => todo with { IsCompleted = true },
        ct: ct);
}
```

### Update with Full Control

```csharp
public async ValueTask TransformAll(CancellationToken ct)
{
    await Items.Update(
        updater: existing => 
            existing
                .Select(item => item with { Name = item.Name.ToUpper() })
                .ToImmutableList(),
        ct: ct);
}
```

### Update Specific Item (with Key)

For types implementing `IKeyEquatable<T>`:

```csharp
public partial record TodoItem([property: Key] Guid Id, string Title, bool IsCompleted);

public async ValueTask UpdateTodo(TodoItem item, CancellationToken ct)
{
    await Todos.UpdateItemAsync(
        oldItem: item,
        updater: todo => todo with { IsCompleted = true },
        ct: ct);
}
```

Or replace directly:

```csharp
public async ValueTask ReplaceTodo(TodoItem oldItem, TodoItem newItem, CancellationToken ct)
{
    await Todos.UpdateItemAsync(
        oldItem: oldItem,
        newItem: newItem,
        ct: ct);
}
```

## Selection Management

### Single Selection

```csharp
public partial record PeopleModel(IPeopleService Service)
{
    public IListFeed<Person> PeopleFeed => 
        ListFeed.Async(Service.GetPeopleAsync);
    
    // Selection state
    public IState<Person?> SelectedPerson => State<Person?>.Empty(this);
    
    // ListState with selection wired
    public IListState<Person> People => 
        PeopleFeed.Selection(SelectedPerson);
}
```

XAML (selection is automatic):

```xml
<ListView ItemsSource="{Binding People}" SelectionMode="Single">
    <ListView.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>

<TextBlock DataContext="{Binding SelectedPerson}"
           Text="{Binding Name}" />
```

### Multi-Selection

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

```xml
<ListView ItemsSource="{Binding People}" SelectionMode="Multiple">
    <ListView.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

### Programmatic Selection

```csharp
// Select specific item
public async ValueTask SelectPerson(Person person, CancellationToken ct)
{
    bool selected = await People.TrySelectAsync(person, ct);
}

// Select multiple items
public async ValueTask SelectPeople(IImmutableList<Person> people, CancellationToken ct)
{
    bool selected = await People.TrySelectAsync(people, ct);
}

// Clear selection
public async ValueTask ClearSelection(CancellationToken ct)
{
    await People.ClearSelection(ct);
}
```

## React to List Changes

```csharp
public TodoModel(ITodoService service)
{
    Service = service;
    
    // Subscribe to list changes
    Todos.ForEach(OnTodosChanged);
}

private async ValueTask OnTodosChanged(IImmutableList<TodoItem> todos, CancellationToken ct)
{
    // Persist changes
    await Service.SaveTodosAsync(todos, ct);
}
```

## Complete Example

### Model

```csharp
public partial record TodoItem([property: Key] Guid Id, string Title, bool IsCompleted);

public partial record TodoModel(ITodoService Service)
{
    public IListState<TodoItem> Todos => 
        ListState.Async(this, Service.GetTodosAsync);
    
    public IState<string> NewTodoTitle => State<string>.Empty(this);
    
    public async ValueTask AddTodo(CancellationToken ct)
    {
        var title = await NewTodoTitle;
        if (!string.IsNullOrWhiteSpace(title))
        {
            var newTodo = new TodoItem(Guid.NewGuid(), title, false);
            await Todos.AddAsync(newTodo, ct);
            await NewTodoTitle.Set(string.Empty, ct);
        }
    }
    
    public async ValueTask ToggleTodo(TodoItem todo, CancellationToken ct)
    {
        await Todos.UpdateItemAsync(
            oldItem: todo,
            newItem: todo with { IsCompleted = !todo.IsCompleted },
            ct: ct);
    }
    
    public async ValueTask DeleteCompleted(CancellationToken ct)
    {
        await Todos.RemoveAllAsync(t => t.IsCompleted, ct);
    }
}
```

### XAML

```xml
<Page x:Class="MyApp.TodoPage"
      xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    
    <StackPanel Spacing="8" Padding="16">
        <!-- Add new todo -->
        <Grid ColumnDefinitions="*,Auto">
            <TextBox Text="{Binding NewTodoTitle, Mode=TwoWay}" 
                     PlaceholderText="Enter todo..." />
            <Button Grid.Column="1" Content="Add" 
                    Command="{Binding AddTodo}" />
        </Grid>
        
        <!-- Todo list -->
        <mvux:FeedView Source="{Binding Todos}">
            <DataTemplate>
                <ListView ItemsSource="{Binding Data}">
                    <ListView.ItemTemplate>
                        <DataTemplate>
                            <Grid ColumnDefinitions="Auto,*">
                                <CheckBox IsChecked="{Binding IsCompleted}" />
                                <TextBlock Grid.Column="1" 
                                           Text="{Binding Title}" />
                            </Grid>
                        </DataTemplate>
                    </ListView.ItemTemplate>
                </ListView>
            </DataTemplate>
        </mvux:FeedView>
        
        <!-- Actions -->
        <Button Content="Clear Completed" 
                Command="{Binding DeleteCompleted}" />
    </StackPanel>
</Page>
```

## Best Practices

1. **Use ListState** when you need to modify the collection
2. **Use Key attribute** on record properties for efficient updates
3. **Use Selection()** operator to wire selection to a state
4. **Use IImmutableList<T>** for multi-selection states
5. **Use ForEach** to persist changes when list updates
6. **Use UpdateItemAsync** with key-equatable types for targeted updates
7. **Prefer AddAsync/RemoveAllAsync** over full Update when possible
