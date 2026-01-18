---
name: mvux-records
description: Use immutable records effectively with MVUX. Use when designing data models, understanding MVUX code generation requirements, or implementing key equality for efficient updates.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# Working with Records in MVUX

MVUX is designed around immutable data using C# records, enabling efficient change detection and predictable state management.

## Prerequisites

- Add `MVUX` to `<UnoFeatures>` in your `.csproj`

```xml
<UnoFeatures>MVUX</UnoFeatures>
```

## Why Records?

Records provide:
- **Immutability:** Values don't change after creation
- **Value equality:** Compared by content, not reference
- **With expressions:** Easy creation of modified copies
- **Deconstruction:** Pattern matching support

## Basic Record Syntax

### Positional Record

```csharp
// Primary constructor with automatic properties
public record Person(string FirstName, string LastName, int Age);
```

Equivalent to:

```csharp
public record Person
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public int Age { get; init; }
    
    public Person(string firstName, string lastName, int age)
    {
        FirstName = firstName;
        LastName = lastName;
        Age = age;
    }
}
```

### Creating Instances

```csharp
var person = new Person("John", "Doe", 30);
```

### Modifying with `with` Expression

```csharp
// Creates new instance with modified property
var olderPerson = person with { Age = 31 };

// Original unchanged
Console.WriteLine(person.Age);      // 30
Console.WriteLine(olderPerson.Age); // 31
```

## MVUX Model Records

### Model Naming Convention

Name your model class with a `Model` suffix:

```csharp
// MainModel -> generates MainViewModel
public partial record MainModel(IWeatherService WeatherService)
{
    public IFeed<Weather> CurrentWeather => 
        Feed.Async(WeatherService.GetCurrentAsync);
}
```

### Required: `partial` Modifier

Always mark models as `partial` for code generation:

```csharp
// ✅ Correct
public partial record ProductModel(IProductService Service) { }

// ❌ Won't generate ViewModel
public record ProductModel(IProductService Service) { }
```

### Dependency Injection via Constructor

```csharp
public partial record OrderModel(
    IOrderService OrderService,
    IPaymentService PaymentService,
    IShippingService ShippingService)
{
    // Services available as properties
    public IFeed<Order> CurrentOrder => 
        Feed.Async(OrderService.GetCurrentAsync);
}
```

## Entity Records

### Basic Entity

```csharp
public record Product(string Id, string Name, decimal Price);
```

### Nested Records

```csharp
public record Address(string Street, string City, string ZipCode);

public record Customer(string Id, string Name, Address ShippingAddress);
```

### Optional/Nullable Properties

```csharp
public record OrderItem(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal? DiscountPrice);  // Nullable
```

### Default Values

```csharp
public record Settings(
    bool DarkMode = false,
    string Language = "en",
    int PageSize = 20);
```

## Key Equality for Collections

For efficient list updates, implement `IKeyEquatable<T>`:

### Using Key Attribute

```csharp
// Uno.Extensions.Equality generates IKeyEquatable implementation
public partial record Product([property: Key] string Id, string Name, decimal Price);
```

### Multiple Key Properties

```csharp
public partial record OrderLine(
    [property: Key] string OrderId,
    [property: Key] int LineNumber,
    string ProductName,
    int Quantity);
```

### Usage in ListState

```csharp
public partial record ProductModel(IProductService Service)
{
    public IListState<Product> Products => 
        ListState.Async(this, Service.GetProductsAsync);
    
    public async ValueTask UpdateProduct(Product product, CancellationToken ct)
    {
        // Key matching finds correct item to replace
        await Products.UpdateItemAsync(
            oldItem: product,
            newItem: product with { Price = product.Price * 1.1m },
            ct: ct);
    }
}
```

## Two-Way Binding with Records

MVUX automatically creates new record instances when properties change:

```csharp
public partial record UserProfile(string Name, string Email, int Age);

public partial record ProfileModel(IUserService Service)
{
    public IState<UserProfile> Profile => 
        State.Async(this, Service.GetProfileAsync);
}
```

```xml
<!-- MVUX handles creating new UserProfile instances -->
<StackPanel DataContext="{Binding Profile}">
    <TextBox Text="{Binding Name, Mode=TwoWay}" />
    <TextBox Text="{Binding Email, Mode=TwoWay}" />
    <NumberBox Value="{Binding Age, Mode=TwoWay}" />
</StackPanel>
```

## Record Collections

### Immutable Collections

```csharp
using System.Collections.Immutable;

public record ShoppingCart(
    IImmutableList<CartItem> Items,
    decimal Total);

public record CartItem(string ProductId, string Name, int Quantity, decimal Price);
```

### In Services

```csharp
public interface ICartService
{
    ValueTask<IImmutableList<CartItem>> GetItemsAsync(CancellationToken ct);
}
```

## Inheritance with Records

```csharp
public abstract record Notification(string Id, DateTime Timestamp, string Message);

public record InfoNotification(string Id, DateTime Timestamp, string Message) 
    : Notification(Id, Timestamp, Message);

public record AlertNotification(string Id, DateTime Timestamp, string Message, string Severity) 
    : Notification(Id, Timestamp, Message);
```

## Record Validation

### With Data Annotations

```csharp
using System.ComponentModel.DataAnnotations;

public record ContactForm(
    [Required, StringLength(100)] string Name,
    [Required, EmailAddress] string Email,
    [Phone] string? Phone,
    [Required, MinLength(10)] string Message);
```

### Custom Validation in Model

```csharp
public partial record ContactModel
{
    public IState<ContactForm> Form => 
        State.Value(this, () => new ContactForm("", "", null, ""));
    
    public IFeed<bool> IsValid => 
        Form.Select(f => 
            !string.IsNullOrWhiteSpace(f.Name) &&
            !string.IsNullOrWhiteSpace(f.Email) &&
            f.Message.Length >= 10);
}
```

## Complete Example

### Entity Records

```csharp
// With Key for efficient updates
public partial record Todo([property: Key] Guid Id, string Title, bool IsCompleted);

// Nested record for filtering
public record TodoFilter(string? SearchText, bool ShowCompleted);
```

### Model Record

```csharp
public partial record TodoModel(ITodoService Service)
{
    // Filter state
    public IState<TodoFilter> Filter => 
        State.Value(this, () => new TodoFilter(null, true));
    
    // List state with async loading
    public IListState<Todo> Todos => 
        ListState.Async(this, Service.GetTodosAsync);
    
    // Filtered view
    public IListFeed<Todo> FilteredTodos => 
        Feed.Combine(Todos, Filter)
            .Select((todos, filter) => todos
                .Where(t => filter.ShowCompleted || !t.IsCompleted)
                .Where(t => string.IsNullOrEmpty(filter.SearchText) || 
                           t.Title.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase))
                .ToImmutableList())
            .AsListFeed();
    
    // New todo input
    public IState<string> NewTodoTitle => State<string>.Empty(this);
    
    public async ValueTask AddTodo(CancellationToken ct)
    {
        var title = await NewTodoTitle;
        if (!string.IsNullOrWhiteSpace(title))
        {
            var todo = new Todo(Guid.NewGuid(), title, false);
            await Todos.AddAsync(todo, ct);
            await NewTodoTitle.Set(string.Empty, ct);
        }
    }
    
    public async ValueTask ToggleTodo(Todo todo, CancellationToken ct)
    {
        // 'with' creates new record with toggled completion
        await Todos.UpdateItemAsync(
            oldItem: todo,
            newItem: todo with { IsCompleted = !todo.IsCompleted },
            ct: ct);
    }
    
    public async ValueTask DeleteTodo(Todo todo, CancellationToken ct)
    {
        await Todos.RemoveAllAsync(t => t.Id == todo.Id, ct);
    }
}
```

## Best Practices

1. **Always use `partial`** on Model records for code generation
2. **Use positional records** for concise syntax
3. **Add `Model` suffix** to trigger ViewModel generation
4. **Use `[property: Key]`** for efficient list updates
5. **Prefer IImmutableList<T>** for collection properties
6. **Use `with` expressions** for modifications
7. **Inject services** via primary constructor
8. **Keep records simple** - no complex logic
9. **Use nullable types** for optional properties
10. **Implement IKeyEquatable** via Key attribute for collections
