---
name: mvux-state-basics
description: Create and use IState<T> for mutable reactive data in MVUX. Use when implementing two-way binding, accepting user input, or maintaining editable application state.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# MVUX State Basics

States (`IState<T>`) are mutable reactive values that support two-way data binding, enabling user input and state updates.

## Prerequisites

- Add `MVUX` to `<UnoFeatures>` in your `.csproj`

```xml
<UnoFeatures>MVUX</UnoFeatures>
```

## What is a State?

An `IState<T>` represents:
- A mutable reactive value
- Two-way data binding support
- Initial value from sync/async source
- Ability to update programmatically

## State vs Feed

| Aspect | IFeed<T> | IState<T> |
|--------|----------|-----------|
| Mutability | Read-only | Read/Write |
| Binding | One-way | Two-way |
| Purpose | Display data | Accept input |
| Caching | No | Yes |

## Creating States

### Empty State

```csharp
using Uno.Extensions.Reactive;

public partial record SearchModel
{
    // Empty state - starts with no value
    public IState<string> SearchTerm => State<string>.Empty(this);
}
```

### State with Initial Value

```csharp
public partial record SettingsModel
{
    // State with initial value
    public IState<int> Volume => State.Value(this, () => 50);
    
    public IState<bool> IsDarkMode => State.Value(this, () => false);
}
```

### State from Async Source

```csharp
public partial record ProfileModel(IUserService Service)
{
    // State loaded asynchronously
    public IState<UserProfile> Profile => 
        State.Async(this, Service.GetProfileAsync);
}
```

### State from AsyncEnumerable

```csharp
public partial record SensorModel(ISensorService Service)
{
    // State updated from push-based source
    public IState<double> Temperature => 
        State.AsyncEnumerable(this, Service.GetTemperatureUpdates);
}
```

### State from Feed

```csharp
public partial record ProductModel(IProductService Service)
{
    private IFeed<Product> ProductFeed => 
        Feed.Async(Service.GetProductAsync);
    
    // Convert feed to editable state
    public IState<Product> Product => 
        State.FromFeed(this, ProductFeed);
}
```

## Two-Way Binding in XAML

### Basic Two-Way Binding

```xml
<TextBox Text="{Binding SearchTerm, Mode=TwoWay}" />
```

### With UpdateSourceTrigger

```xml
<TextBox Text="{Binding SearchTerm, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
```

### Binding to Record Properties

When the state holds a record, you can bind to its properties:

```csharp
public partial record UserProfile(string Name, string Email);

public partial record ProfileModel(IUserService Service)
{
    public IState<UserProfile> Profile => 
        State.Async(this, Service.GetProfileAsync);
}
```

```xml
<!-- MVUX handles creating new record instances on property changes -->
<StackPanel DataContext="{Binding Profile}">
    <TextBox Text="{Binding Name, Mode=TwoWay}" />
    <TextBox Text="{Binding Email, Mode=TwoWay}" />
</StackPanel>
```

## Reading State in Code

### Await Current Value

```csharp
public async ValueTask Save(CancellationToken ct)
{
    // Await the state to get current value
    var profile = await Profile;
    if (profile is not null)
    {
        await _service.SaveProfileAsync(profile, ct);
    }
}
```

## Updating State Programmatically

### Set New Value

```csharp
public async ValueTask ClearSearch(CancellationToken ct)
{
    await SearchTerm.Set(string.Empty, ct);
}
```

### Update with Transformation

```csharp
public async ValueTask IncrementVolume(CancellationToken ct)
{
    await Volume.Update(current => current + 10, ct);
}
```

### Update Record State

```csharp
public async ValueTask UpdateEmail(string newEmail, CancellationToken ct)
{
    await Profile.Update(
        profile => profile with { Email = newEmail }, 
        ct);
}
```

## Derived Feeds from State

Create derived feeds that react to state changes:

```csharp
public partial record SearchModel(ISearchService Service)
{
    public IState<string> SearchTerm => State<string>.Empty(this);
    
    // Feed that reacts to state changes
    public IFeed<SearchResults> Results => 
        SearchTerm.SelectAsync(async (term, ct) => 
            await Service.SearchAsync(term, ct));
}
```

## ForEach - React to State Changes

```csharp
public partial record SettingsModel(ISettingsService Service)
{
    public IState<int> Volume => State.Value(this, () => 50);
    
    public SettingsModel(ISettingsService service)
    {
        Service = service;
        
        // React when Volume changes
        Volume.ForEach(OnVolumeChanged);
    }
    
    private async ValueTask OnVolumeChanged(int volume, CancellationToken ct)
    {
        await Service.SaveVolumeAsync(volume, ct);
    }
}
```

## Complete Example

### Model

```csharp
using Uno.Extensions.Reactive;

public partial record Person(string FirstName, string LastName);

public partial record PersonEditorModel(IPersonService Service)
{
    // Async-loaded state
    public IState<Person> Person => 
        State.Async(this, Service.GetPersonAsync);
    
    // Save command
    public async ValueTask Save(CancellationToken ct)
    {
        var person = await Person;
        if (person is not null)
        {
            await Service.SavePersonAsync(person, ct);
        }
    }
}
```

### XAML

```xml
<Page x:Class="MyApp.PersonEditorPage"
      xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    
    <mvux:FeedView Source="{Binding Person}">
        <mvux:FeedView.ValueTemplate>
            <DataTemplate>
                <StackPanel Spacing="8">
                    <TextBox Header="First Name" 
                             Text="{Binding FirstName, Mode=TwoWay}" />
                    <TextBox Header="Last Name" 
                             Text="{Binding LastName, Mode=TwoWay}" />
                    <Button Content="Save" 
                            Command="{Binding Parent.Save}" />
                </StackPanel>
            </DataTemplate>
        </mvux:FeedView.ValueTemplate>
        
        <mvux:FeedView.ProgressTemplate>
            <DataTemplate>
                <ProgressRing IsActive="True" />
            </DataTemplate>
        </mvux:FeedView.ProgressTemplate>
    </mvux:FeedView>
</Page>
```

## State with Complex Types

### Using with DateTimeOffset

```csharp
public IState<DateTimeOffset> SelectedDate => 
    State.Value(this, () => DateTimeOffset.Now);
```

### Using with Enum

```csharp
public enum Priority { Low, Medium, High }

public IState<Priority> TaskPriority => 
    State.Value(this, () => Priority.Medium);
```

### Using with Nullable

```csharp
public IState<int?> OptionalValue => State<int?>.Empty(this);
```

## Best Practices

1. **Use State.Empty** for user input fields with no initial value
2. **Use State.Value** for settings/preferences with defaults
3. **Use State.Async** to load editable data from a service
4. **Always pass `this`** as the owner (first parameter)
5. **Use records** for complex state types (immutable by nature)
6. **Use ForEach** for side effects when state changes
7. **Use Mode=TwoWay** in XAML bindings for user input
8. **Use UpdateSourceTrigger=PropertyChanged** for immediate updates
