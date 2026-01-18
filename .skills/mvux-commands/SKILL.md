---
name: mvux-commands
description: Create and use commands in MVUX for user interactions. Use when implementing button actions, handling method invocations from UI, or configuring command generation with feed parameters.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# MVUX Commands

MVUX automatically generates commands from public methods in your Model, enabling easy binding to UI actions.

## Prerequisites

- Add `MVUX` to `<UnoFeatures>` in your `.csproj`

```xml
<UnoFeatures>MVUX</UnoFeatures>
```

## Automatic Command Generation

### Basic Command

Any public method in a Model becomes a command:

```csharp
public partial record MainModel
{
    public void Save()
    {
        // Save logic
    }
}
```

Generated ViewModel:

```csharp
public partial class MainViewModel
{
    public IAsyncCommand Save { get; }
}
```

XAML binding:

```xml
<Button Content="Save" Command="{Binding Save}" />
```

### Async Command

Async methods automatically disable the button while executing:

```csharp
public partial record MainModel(IDataService Service)
{
    public async ValueTask LoadData(CancellationToken ct)
    {
        await Service.LoadAsync(ct);
    }
}
```

```xml
<!-- Button auto-disables while LoadData runs -->
<Button Content="Load" Command="{Binding LoadData}" />
```

### Command with CancellationToken

Always include `CancellationToken` as the last parameter:

```csharp
public partial record MainModel
{
    // CancellationToken is cancelled when ViewModel is disposed
    public async ValueTask RefreshData(CancellationToken ct)
    {
        await _service.RefreshAsync(ct);
    }
}
```

## Command Parameters

### From CommandParameter

```csharp
public partial record MainModel
{
    public void ApplyDiscount(double percentage)
    {
        // Apply percentage discount
    }
}
```

```xml
<Slider x:Name="DiscountSlider" Minimum="0" Maximum="50" />
<Button Content="Apply Discount" 
        Command="{Binding ApplyDiscount}"
        CommandParameter="{Binding Value, ElementName=DiscountSlider}" />
```

**Note:** If CommandParameter is null or wrong type, the button is disabled.

### With CancellationToken

```csharp
public async ValueTask ApplyDiscount(double percentage, CancellationToken ct)
{
    await _service.ApplyDiscountAsync(percentage, ct);
}
```

## Feed Parameters

MVUX can automatically inject current feed values as command parameters.

### Implicit Feed Parameter Matching

Parameters are matched by name and type:

```csharp
public partial record CounterModel
{
    public IFeed<int> CounterValue => Feed.Async(async ct => await GetCounterAsync(ct));
    
    // 'counterValue' matches 'CounterValue' feed by name (case-insensitive)
    public void ResetCounter(int counterValue)
    {
        // counterValue contains current feed value
    }
}
```

```xml
<Button Content="Reset" Command="{Binding ResetCounter}" />
```

### Explicit Feed Parameter

Use `[FeedParameter]` when names don't match:

```csharp
public partial record CounterModel
{
    public IFeed<int> CounterValue => Feed.Async(GetCounterAsync);
    
    [ImplicitFeedCommandParameter(false)]
    public void ResetCounter([FeedParameter(nameof(CounterValue))] int newValue)
    {
        // newValue contains CounterValue's current value
    }
}
```

## Configuring Command Generation

### Disable for Single Method

```csharp
public partial record MainModel
{
    [Command(false)]
    public async ValueTask Save()
    {
        // This becomes a method, not a command
    }
}
```

Use method binding instead:

```xml
<Button Click="{x:Bind Save}" Content="Save" />
```

### Force Command Generation

```csharp
[assembly: ImplicitCommands(false)]

public partial record MainModel
{
    [Command] // Explicitly enable command
    public async ValueTask Save()
    {
        // This generates a command
    }
}
```

### Disable for Class

```csharp
[ImplicitCommands(false)]
public partial record MainModel
{
    public void DoWork() { } // No command generated
}
```

### Disable for Assembly

```csharp
[assembly: ImplicitCommands(false)]
```

### Disable Feed Parameter Matching

```csharp
[ImplicitFeedCommandParameter(false)]
public partial record MainModel
{
    public IFeed<int> Value => ...;
    
    // 'value' won't auto-match to Value feed
    public void ProcessValue(int value) { }
}
```

## Explicit Command Creation

For advanced scenarios, create commands manually:

### Command.Async

```csharp
public partial record MainModel(IServerService Server)
{
    public ICommand PingCommand => 
        Command.Async(async ct => await Server.PingAsync(ct));
}
```

Bind via Model property:

```xml
<Button Content="Ping" Command="{Binding Model.PingCommand}" />
```

### Command with Feed and Condition

```csharp
public partial record PagerModel
{
    public IFeed<int> CurrentPage => ...;
    
    public IAsyncCommand GoToPageCommand =>
        Command.Create(builder =>
            builder
                .Given(CurrentPage)
                .When(page => page > 0)
                .Then(async (page, ct) => await NavigateToPage(page, ct)));
    
    private ValueTask NavigateToPage(int page, CancellationToken ct) => ...;
}
```

```xml
<Button Content="Go" Command="{Binding Model.GoToPageCommand}" />
```

## Command with XAML Behaviors

Execute commands on any event:

```xml
<Page xmlns:interactivity="using:Microsoft.Xaml.Interactivity"
      xmlns:interactions="using:Microsoft.Xaml.Interactions.Core">
    
    <TextBlock x:Name="TitleBlock" Text="Double-tap me">
        <interactivity:Interaction.Behaviors>
            <interactions:EventTriggerBehavior EventName="DoubleTapped">
                <interactions:InvokeCommandAction
                    Command="{Binding OnDoubleTapped}"
                    CommandParameter="{Binding Text, ElementName=TitleBlock}" />
            </interactions:EventTriggerBehavior>
        </interactivity:Interaction.Behaviors>
    </TextBlock>
</Page>
```

Model:

```csharp
public partial record MainModel
{
    public void OnDoubleTapped(string text)
    {
        // Handle double-tap
    }
}
```

## Command Generation Rules

A command is generated when:
1. Method is **public**
2. Returns `void`, `Task`, or `ValueTask`
3. Has optional `CancellationToken` as last parameter
4. Has optional feed-matched parameters
5. Has optional single CommandParameter

## Complete Example

```csharp
public partial record ProductModel(IProductService Service)
{
    public IFeed<Product> Product => Feed.Async(Service.GetProductAsync);
    
    public IState<int> Quantity => State.Value(this, () => 1);
    
    // Uses Quantity state value automatically (name match)
    public async ValueTask AddToCart(int quantity, CancellationToken ct)
    {
        var product = await Product;
        if (product is not null && quantity > 0)
        {
            await Service.AddToCartAsync(product.Id, quantity, ct);
        }
    }
    
    // Takes CommandParameter from XAML
    public async ValueTask SelectVariant(string variantId, CancellationToken ct)
    {
        await Service.SelectVariantAsync(variantId, ct);
    }
}
```

```xml
<StackPanel>
    <NumberBox Value="{Binding Quantity, Mode=TwoWay}" />
    <Button Content="Add to Cart" Command="{Binding AddToCart}" />
    
    <ListView ItemsSource="{Binding Product.Variants}">
        <ListView.ItemTemplate>
            <DataTemplate>
                <Button Content="{Binding Name}"
                        Command="{Binding DataContext.SelectVariant, 
                                  RelativeSource={RelativeSource AncestorType=Page}}"
                        CommandParameter="{Binding Id}" />
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</StackPanel>
```

## Best Practices

1. **Always include CancellationToken** as last parameter in async methods
2. **Use feed parameters** for automatic value injection
3. **Use [Command(false)]** when you need method binding via x:Bind
4. **Use IAsyncCommand** for explicit commands with feed dependencies
5. **Avoid return values** in command methods (they are ignored)
6. **Match parameter names** to feed names for implicit injection
