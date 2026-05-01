---
uid: Uno.Extensions.Mvux.HowToCustomControls
---

# How to use custom controls with MVUX

This guide covers how to build custom controls with DependencyProperties that integrate cleanly with MVUX models, including correct property typing, event handling, wrapping platform controls, and accessing configuration.

> [!TIP]
> This guide assumes you are already familiar with [MVUX fundamentals](xref:Uno.Extensions.Mvux.Overview), [feeds](xref:Uno.Extensions.Mvux.HowToSimpleFeed), and [states](xref:Uno.Extensions.Mvux.HowToSimpleState).

## The layer separation principle

A custom control and an MVUX model serve different roles and should never be coupled directly:

| Layer | Responsibility | Knows about MVUX? |
|-------|---------------|-------------------|
| **Custom control** | Renders UI, exposes DependencyProperties, raises events | No |
| **MVUX Model** | Composes feeds/states from services, exposes commands | Yes |
| **Generated ViewModel** | Bridges the model to the data-binding engine | Auto-generated |

The custom control binds to the **generated ViewModel** through standard data binding — the same way any built-in WinUI control does. There is no special MVUX integration required inside the control itself.

## 1. Defining DependencyProperties for MVUX consumption

### Use `IEnumerable<T>` for collection properties

When your custom control **displays** a collection owned by the Model (sourced from an `IListFeed<T>` or `IListState<T>`), type your DependencyProperty as `IEnumerable<T>` — **not** `ObservableCollection<T>`:

> [!NOTE]
> This guidance applies when the **Model owns the collection** and the control only reads it for display.
> If your control internally manages its own collection (e.g., a game loop spawning and removing entities), the control should own a private `ObservableCollection<T>` internally and the DependencyProperty serves only as an external binding surface. See [Section 4](#4-wrapping-platform-controls-contentdialog-webview2-skcanvaselement) for imperative/rendering controls that manage their own state.

```csharp
public sealed partial class LootDisplay : Control
{
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(IEnumerable<Loot>),
            typeof(LootDisplay),
            new PropertyMetadata(null, OnItemsChanged));

    public IEnumerable<Loot> Items
    {
        get => (IEnumerable<Loot>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LootDisplay)d;
        control.RebuildVisuals();

        // Subscribe to collection changes if available
        if (e.OldValue is INotifyCollectionChanged oldNcc)
            oldNcc.CollectionChanged -= control.OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newNcc)
            newNcc.CollectionChanged += control.OnCollectionChanged;
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildVisuals();
    }

    private void RebuildVisuals()
    {
        // Update internal rendering based on Items
    }
}
```

**Why `IEnumerable<T>` and not `List<T>`?** The MVUX code generator produces a bindable collection that implements `INotifyCollectionChanged` but does **not** implement `IList<T>`. Using `List<T>` as the DP type would reject the MVUX-generated collection at runtime. `IEnumerable<T>` is the widest compatible interface — it accepts the MVUX-generated collection, `ObservableCollection<T>`, and a plain `List<T>` in design-time data. If you need indexed access inside the control, cast to `IList` or call `.ToList()` in the property-changed callback.

### Use the entity type directly for single-value properties

For properties driven by `IFeed<T>` or `IState<T>`, type the DP to the entity type:

```csharp
public static readonly DependencyProperty DifficultyProperty =
    DependencyProperty.Register(
        nameof(Difficulty),
        typeof(DifficultyLevel),
        typeof(GameBoard),
        new PropertyMetadata(DifficultyLevel.Normal, OnDifficultyChanged));

public DifficultyLevel Difficulty
{
    get => (DifficultyLevel)GetValue(DifficultyProperty);
    set => SetValue(DifficultyProperty, value);
}
```

## 2. Binding the custom control from XAML

In the consuming page, bind the control's DependencyProperties to the MVUX-generated ViewModel properties.

The examples below use a `GameModel` that composes data for the `LootDisplay` and `GameBoard` custom controls defined in Section 1.

### The Model

```csharp
public partial record GameModel(ILootService LootService)
{
    public IListState<Loot> Loot => ListState.Async(this, LootService.GetLootAsync);

    public IState<DifficultyLevel> Difficulty => State.Value(this, () => DifficultyLevel.Normal);
}
```

### The XAML

```xml
<Page x:Class="MyApp.Presentation.GamePage"
      xmlns:controls="using:MyApp.Controls">

    <controls:LootDisplay Items="{Binding Loot}"
                           Difficulty="{Binding Difficulty, Mode=TwoWay}" />
</Page>
```

No special wiring is needed. The generated `GameViewModel` exposes `Loot` as a bindable collection and `Difficulty` as a bindable property. The custom control receives them through its DependencyProperties.

> [!TIP]
> MVUX auto-generates a ViewModel for each `partial class` or `record` named with a `Model` suffix. Since V5, the generated class uses the `*ViewModel` naming convention (e.g., `GameModel` -> `GameViewModel`). The older `Bindable*Model` naming from V4 is deprecated. See [Upgrading MVUX](xref:Uno.Extensions.Mvux.ReactiveMigration) for details.

### Using FeedView for loading and error states

If your control should display loading or error states, wrap it in a `FeedView`:

```xml
<uer:FeedView Source="{Binding Loot}">
    <uer:FeedView.ValueTemplate>
        <DataTemplate>
            <controls:LootDisplay Items="{Binding Data}" />
        </DataTemplate>
    </uer:FeedView.ValueTemplate>
    <uer:FeedView.ProgressTemplate>
        <DataTemplate>
            <ProgressRing IsActive="True" />
        </DataTemplate>
    </uer:FeedView.ProgressTemplate>
</uer:FeedView>
```

## 3. Events in custom controls

Custom controls should use standard CLR events or routed events for control-internal behavior. This is normal WinUI practice and requires no MVUX-specific patterns:

```csharp
public sealed partial class GameBoard : Control
{
    public event EventHandler<RunCompletedEventArgs> RunCompleted;

    private void OnRunFinished(int score)
    {
        RunCompleted?.Invoke(this, new RunCompletedEventArgs(score));
    }
}

public sealed class RunCompletedEventArgs : EventArgs
{
    public int Score { get; }
    public RunCompletedEventArgs(int score) => Score = score;
}
```

### Connecting events to MVUX commands

There are three approaches, depending on your needs:

**Approach A: Code-behind handler calls into the ViewModel**

```csharp
// GamePage.xaml.cs
private void GameBoard_RunCompleted(object sender, RunCompletedEventArgs e)
{
    // The DataContext is the generated ViewModel, which exposes model methods as commands
    if (DataContext is GameViewModel vm)
    {
        vm.HandleRunCompleted(e.Score);
    }
}
```

**Approach B: Use Uno Toolkit `CommandExtensions`**

If the control raises the event and you want zero code-behind, use `CommandExtensions` from Uno Toolkit to bind an event to a command declaratively. See the [CommandExtensions documentation](xref:Toolkit.Helpers.CommandExtensions).

**Approach C: Expose a command DP on the control**

For reusable library controls, expose a command DependencyProperty so consumers can bind directly:

```csharp
public static readonly DependencyProperty RunCompletedCommandProperty =
    DependencyProperty.Register(
        nameof(RunCompletedCommand),
        typeof(ICommand),
        typeof(GameBoard),
        new PropertyMetadata(null));

public ICommand RunCompletedCommand
{
    get => (ICommand)GetValue(RunCompletedCommandProperty);
    set => SetValue(RunCompletedCommandProperty, value);
}

private void OnRunFinished(int score)
{
    RunCompleted?.Invoke(this, new RunCompletedEventArgs(score));
    RunCompletedCommand?.Execute(score);
}
```

```xml
<controls:GameBoard RunCompletedCommand="{Binding HandleRunCompleted}" />
```

## 4. Wrapping platform controls (ContentDialog, WebView2, SKCanvasElement)

Platform controls that are **rendering surfaces** (WebView2, SKCanvasElement) or **modal UI** (ContentDialog) require a different pattern than data-bound controls. They do not fit the standard DP-binding model because they are driven by imperative APIs.

> [!IMPORTANT]
> If your custom control **internally owns and mutates its own collections** (e.g., a game loop that spawns/removes entities on a canvas), it falls into this category — not Section 1. The control should manage its state with private `ObservableCollection<T>` fields internally and expose DependencyProperties only for configuration or external data that flows in from the Model.

### Pattern: Service-driven control registration

This pattern is used in the [Matrix app](https://github.com/mtmattei/matrix) for its `MatrixTransitionOverlay`:

**Step 1: Define a service interface**

```csharp
public interface IOverlayService
{
    Task ShowOverlayAsync(OverlayOptions options, CancellationToken ct = default);
    void RegisterOverlay(MyOverlayControl control);
}
```

**Step 2: Place the control in Shell.xaml (or a high-level page)**

```xml
<Grid>
    <Frame uen:Region.Attached="true" />
    <controls:MyOverlayControl x:Name="Overlay" />
</Grid>
```

**Step 3: Register during Loaded**

```csharp
// Shell.xaml.cs
private async void Shell_Loaded(object sender, RoutedEventArgs e)
{
    var host = ((App)App.Current).Host;
    var overlayService = host.Services.GetRequiredService<IOverlayService>();
    overlayService.RegisterOverlay(Overlay);
}
```

**Step 4: Call from any Model**

```csharp
public partial record GameModel(IOverlayService OverlayService)
{
    public async ValueTask ShowVictoryOverlay(CancellationToken ct)
    {
        await OverlayService.ShowOverlayAsync(new OverlayOptions(Duration: TimeSpan.FromSeconds(3)), ct);
    }
}
```

### ContentDialog with Navigation Extensions

For ContentDialog, prefer [Navigation Extensions](xref:Uno.Extensions.Navigation.HowToShowDialog) over the service pattern:

```csharp
// From a model method (INavigator injected via constructor)
await Navigator.NavigateViewAsync<ConfirmDeleteDialog>(this, qualifier: Qualifiers.Dialog);
```

Or declaratively from XAML using the `!` prefix:

```xml
<Button Content="Delete" uen:Navigation.Request="!ConfirmDelete" />
```

## 5. Accessing configuration (IOptions&lt;T&gt;) from MVUX

Configuration values should flow through the **Model** or **Service** layer, never directly into the view.

### Inject into the Model

```csharp
public partial record GameModel(
    IOptions<GameConfig> Config,
    ILootService LootService)
{
    public IState<double> Speed =>
        State.Value(this, () => Config.Value.Speed);

    public IListState<Loot> Loot =>
        ListState.Async(this, LootService.GetLootAsync);
}
```

The view binds to `Speed` like any other state:

```xml
<controls:GameBoard Speed="{Binding Speed}" />
```

### For runtime-changeable configuration

Use `IWritableOptions<T>` when the user can change settings:

```csharp
public partial record SettingsModel(IWritableOptions<GameConfig> Config)
{
    public IState<DifficultyLevel> Difficulty =>
        State.Value(this, () => Config.Value.Difficulty);

    public async ValueTask SetDifficulty(DifficultyLevel level, CancellationToken ct)
    {
        await Config.UpdateAsync(c => c with { Difficulty = level });
    }
}
```

### Prefer injecting into Services

For configuration that drives business logic (API keys, feature flags, thresholds), inject `IOptions<T>` into the service rather than the model:

```csharp
public class LootService : ILootService
{
    private readonly GameConfig _config;

    public LootService(IOptions<GameConfig> config)
    {
        _config = config.Value;
    }

    public async ValueTask<IImmutableList<Loot>> GetLootAsync(CancellationToken ct)
    {
        // Use _config.DropRate, _config.MaxItems, etc.
    }
}
```

The model stays focused on composing feeds/states, and the service owns the business rules that depend on configuration.

## Summary

| Scenario | Pattern |
|----------|---------|
| Collection DP on custom control (Model-owned data) | Type as `IEnumerable<T>`, subscribe to `INotifyCollectionChanged` |
| Collection managed internally by the control | Private `ObservableCollection<T>`, service-driven pattern (Section 4) |
| Single-value DP | Type to the entity type directly |
| Control events -> Model | Code-behind handler, `CommandExtensions`, or command DP |
| Rendering surfaces (WebView2, SKCanvas) | Service-driven registration pattern |
| ContentDialog | Navigation Extensions with `Qualifiers.Dialog` |
| Configuration in Model | Inject `IOptions<T>` or `IWritableOptions<T>` |
| Configuration for business logic | Inject `IOptions<T>` into the Service |
