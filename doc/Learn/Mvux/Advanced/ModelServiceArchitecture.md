---
uid: Uno.Extensions.Mvux.Advanced.ModelServiceArchitecture
---

# Model vs Service architecture in MVUX

MVUX applications have two key layers below the view: **Models** and **Services**. The MVUX documentation and samples use services extensively, but the decision of what belongs in each layer is not always obvious. This guide provides a clear framework.

## The two layers

### Model

A Model is a `partial record` that the MVUX code generator processes. It exposes reactive data (`IFeed<T>`, `IState<T>`) and commands (public methods) to the view.

```csharp
public partial record InventoryModel(ILootService LootService, INavigator Navigator)
{
    public IListState<Loot> Loot => ListState.Async(this, LootService.GetLootAsync);

    public IState<string> SearchTerm => State<string>.Empty(this);

    public IListFeed<Loot> FilteredLoot => SearchTerm.SelectAsync(LootService.SearchAsync).AsListFeed();

    public async ValueTask DeleteItem(Loot item, CancellationToken ct)
    {
        await LootService.DeleteAsync(item.Id, ct);
        await Loot.RemoveAllAsync(x => x.Id == item.Id, ct);
    }
}
```

**The model is the orchestration layer.** It decides *what data the view sees* and *what happens when the user acts*.

### Service

A Service is a plain class behind an interface. It handles data access, business rules, and external integrations. It returns `ValueTask<T>` or `ValueTask<IImmutableList<T>>` and knows nothing about feeds, states, or UI.

```csharp
public interface ILootService
{
    ValueTask<IImmutableList<Loot>> GetLootAsync(CancellationToken ct);
    ValueTask<IImmutableList<Loot>> SearchAsync(string term, CancellationToken ct);
    ValueTask DeleteAsync(Guid id, CancellationToken ct);
}

public class LootService : ILootService
{
    private readonly HttpClient _http;

    public LootService(HttpClient http) => _http = http;

    public async ValueTask<IImmutableList<Loot>> GetLootAsync(CancellationToken ct)
    {
        var response = await _http.GetFromJsonAsync<List<Loot>>("/api/loot", ct);
        return response?.ToImmutableList() ?? ImmutableList<Loot>.Empty;
    }

    // ...
}
```

**The service is the data/logic layer.** It answers *"how do I get or change this data?"*

## Decision framework

Use this table when deciding where a piece of logic belongs:

| Question | If yes → | If no → |
|----------|----------|---------|
| Does it fetch, persist, or compute data? | **Service** | Keep reading |
| Does it call an external API or database? | **Service** | Keep reading |
| Does it compose multiple data sources for the view? | **Model** | Keep reading |
| Does it react to user input (state changes)? | **Model** | Keep reading |
| Does it trigger navigation? | **Model** | Keep reading |
| Is it a business rule independent of UI? | **Service** | **Model** |

### Rules of thumb

1. **If you can test it without a UI framework, it's a service.** Services depend on `HttpClient`, database contexts, file system — never on feeds, states, or `INavigator`.

2. **If it transforms service data into something the view needs, it's a model.** Filtering, combining, mapping — these are model operations using `Select`, `Where`, and `SelectAsync` on feeds.

3. **If two models need the same logic, extract to a service.** Shared logic lives in services, not base models.

4. **Services are stateless by default.** A service *can* hold internal state (caches, connection pools), but it should not expose reactive `IState` or `IFeed` properties. If a service needs to push updates, return `IAsyncEnumerable<T>` and let the model wrap it in a feed.

## Model composition (instead of inheritance)

MVUX models **cannot inherit from each other** in a meaningful way. The code generator processes each `partial record` with a `Model` suffix independently and generates a ViewModel for its directly declared `IFeed`/`IState` properties. Inherited feed/state properties from a base record are not reliably processed.

### Why not inheritance?

```csharp
// ⚠️ Don't do this — the generator may not process inherited feeds
public partial record BaseItemModel(IItemService Service)
{
    public IListState<Item> Items => ListState.Async(this, Service.GetItemsAsync);
}

public partial record LootModel(IItemService Service) : BaseItemModel(Service)
{
    // Items feed is inherited but may not appear in the generated LootViewModel
}
```

### Use composition through shared services

```csharp
// ✅ Extract shared logic into a service
public interface IItemService
{
    ValueTask<IImmutableList<Loot>> GetLootAsync(CancellationToken ct);
    ValueTask<IImmutableList<Gift>> GetGiftsAsync(CancellationToken ct);
    ValueTask<IImmutableList<T>> SearchAsync<T>(string term, CancellationToken ct) where T : ISearchable;
}

// Each model composes independently from the same service
public partial record LootModel(IItemService ItemService)
{
    public IListState<Loot> Items => ListState.Async(this, ItemService.GetLootAsync);
    public IState<string> Search => State<string>.Empty(this);
}

public partial record GiftModel(IItemService ItemService)
{
    public IListState<Gift> Items => ListState.Async(this, ItemService.GetGiftsAsync);
    public IState<string> Search => State<string>.Empty(this);
}
```

If the two models are nearly identical, that's a signal that you may only need **one model** parameterized by the service call, or that the shared behavior belongs entirely in the service.

## Cross-cutting UI services

Some concerns span multiple pages and don't belong to any single model: transition overlays, toast notifications, theme switching, connectivity banners. These follow the **service-driven control registration** pattern.

### Pattern

1. Define a service interface with the imperative API:

```csharp
public interface IToastService
{
    Task ShowAsync(string message, ToastSeverity severity, CancellationToken ct = default);
    void RegisterHost(ToastHost control);
}
```

2. Place the control at the Shell level:

```xml
<!-- Shell.xaml -->
<Grid>
    <Frame uen:Region.Attached="true" />
    <controls:ToastHost x:Name="ToastHost" />
</Grid>
```

3. Register the control with the service during `Loaded`:

```csharp
// Shell.xaml.cs
private async void OnLoaded(object sender, RoutedEventArgs e)
{
    var toast = App.Current.Host.Services.GetRequiredService<IToastService>();
    toast.RegisterHost(ToastHost);
}
```

4. Any model can inject and use the service:

```csharp
public partial record CheckoutModel(IToastService Toast, IOrderService Orders)
{
    public async ValueTask PlaceOrder(CancellationToken ct)
    {
        await Orders.SubmitAsync(ct);
        await Toast.ShowAsync("Order placed!", ToastSeverity.Success, ct);
    }
}
```

This is the same pattern used in the [Matrix app](https://github.com/mtmattei/matrix) for its `IMatrixTransitionService` and `MatrixTransitionOverlay`.

### When to use this pattern vs Navigation Extensions

| Scenario | Use |
|----------|-----|
| Modal dialog with a result (confirm/cancel) | Navigation Extensions (`Qualifiers.Dialog`) |
| Page-to-page navigation with transitions | Navigation Extensions |
| Overlay/toast/banner that doesn't interrupt navigation | Service-driven registration |
| Complex rendering surface (SkiaSharp, WebView2) | Service-driven registration |

## Stateful services

While services should be **stateless by default**, some scenarios require persistent state that outlives any single model — for example, a real-time connection, a local cache, or a sync engine.

### Push updates with `IAsyncEnumerable<T>`

If a service needs to push updates over time, expose `IAsyncEnumerable<T>` and let the model wrap it:

```csharp
public interface IChatService
{
    IAsyncEnumerable<ChatMessage> GetMessages(string channelId, CancellationToken ct);
    ValueTask SendAsync(string channelId, string text, CancellationToken ct);
}

public partial record ChatModel(IChatService Chat)
{
    public IListFeed<ChatMessage> Messages =>
        ListFeed.AsyncEnumerable(this, ct => Chat.GetMessages("general", ct));
}
```

The service manages the connection; the model exposes it as a reactive feed. The service never references `IFeed` or `IState` — it returns standard .NET async types.

### Cache invalidation with messaging

When a service modifies data that multiple models display, use [MVUX messaging](xref:Uno.Extensions.Mvux.Advanced.Messaging) to notify models:

```csharp
public class LootService : ILootService
{
    private readonly IMessenger _messenger;

    public async ValueTask DeleteAsync(Guid id, CancellationToken ct)
    {
        await _http.DeleteAsync($"/api/loot/{id}", ct);
        _messenger.Send(new EntityMessage<Loot>(EntityChange.Deleted, new Loot(id)));
    }
}
```

Models that subscribe to `EntityMessage<Loot>` will automatically refresh their feeds/states.

## Architecture overview

```
┌──────────────────────────────────────────────────────┐
│  View Layer                                          │
│  ┌─────────────────┐  ┌──────────────────────────┐  │
│  │ Custom Controls  │  │ Pages (XAML)             │  │
│  │ (DPs, events,    │  │ (bind to ViewModel)      │  │
│  │  no MVUX refs)   │  │                          │  │
│  └─────────────────┘  └──────────────────────────┘  │
├──────────────────────────────────────────────────────┤
│  Model Layer (generated ViewModel bridges to View)   │
│  ┌──────────────────────────────────────────────┐    │
│  │ partial record XxxModel(IService, INavigator) │    │
│  │  • IFeed<T> / IState<T> / IListState<T>      │    │
│  │  • Public methods → auto-generated commands   │    │
│  │  • Composes data for the view                 │    │
│  │  • Triggers navigation                        │    │
│  └──────────────────────────────────────────────┘    │
├──────────────────────────────────────────────────────┤
│  Service Layer                                       │
│  ┌─────────────────┐  ┌──────────────────────────┐  │
│  │ Data Services    │  │ Cross-cutting UI Services│  │
│  │ (API, DB, files) │  │ (overlays, toasts)       │  │
│  │ → ValueTask<T>   │  │ → RegisterControl()      │  │
│  │ → IAsyncEnum<T>  │  │ → imperative async API   │  │
│  └─────────────────┘  └──────────────────────────┘  │
├──────────────────────────────────────────────────────┤
│  Infrastructure                                      │
│  IOptions<T> · HttpClient · IMessenger · INavigator  │
└──────────────────────────────────────────────────────┘
```

## See also

- [MVUX Overview](xref:Uno.Extensions.Mvux.Overview)
- [How to use custom controls with MVUX](xref:Uno.Extensions.Mvux.HowToCustomControls)
- [Messaging](xref:Uno.Extensions.Mvux.Advanced.Messaging)
- [Commands](xref:Uno.Extensions.Mvux.Advanced.Commands)
