---
name: uno-navigation-troubleshooting
description: Troubleshoot common issues with Uno Platform Navigation Extensions. Use when encountering navigation errors, unexpected behavior, or configuration problems.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Navigation Troubleshooting

This skill covers diagnosing and resolving common issues with Uno Platform Navigation Extensions.

## Common Setup Issues

### Navigation Not Working At All

**Symptoms:**
- Nothing happens when clicking navigation buttons
- `INavigator` is null
- Routes not found

**Solutions:**

1. **Check UseNavigation in App.xaml.cs:**
```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => host
            .UseNavigation(RegisterRoutes)  // Must be called
        );
    // ...
}
```

2. **Verify UnoFeatures in .csproj:**
```xml
<UnoFeatures>Navigation</UnoFeatures>
```

3. **Check Shell is set:**
```csharp
MainWindow.Content = new Shell();
// or in host configuration
.UseNavigation(RegisterRoutes)
```

### Region.Attached Not Working

**Symptoms:**
- Content not appearing in region
- Navigation requests ignored

**Solutions:**

1. **Ensure parent has Region.Attached:**
```xml
<!-- Root must have Region.Attached -->
<Grid uen:Region.Attached="True">
    <Frame uen:Region.Attached="True" />
</Grid>
```

2. **Check XAML namespace:**
```xml
xmlns:uen="using:Uno.Extensions.Navigation.UI"
```

3. **Verify region hierarchy:**
   - Every navigation container needs `Region.Attached="True"`
   - Parent regions must be attached before children work

## Route Registration Issues

### Route Not Found

**Symptoms:**
- `RouteNotMappedException`
- Navigation returns without showing content

**Solutions:**

1. **Check route name spelling:**
```csharp
// Route registration
new RouteMap("Products", View: views.FindByViewModel<ProductsViewModel>())

// Must match exactly in XAML
<Button uen:Navigation.Request="Products" />  // Case-sensitive
```

2. **Verify view is registered:**
```csharp
views.Register(
    new ViewMap<ProductsPage, ProductsViewModel>()  // Required before RouteMap
);
```

3. **Check nested route path:**
```csharp
// For nested routes
new RouteMap("Main", 
    Nested:
    [
        new RouteMap("Products", ...)  // Navigate with "Products", not "Main/Products"
    ]
)
```

### ViewModel Not Receiving Data

**Symptoms:**
- Constructor parameter is null or default
- Data not passed to ViewModel

**Solutions:**

1. **Use DataViewMap instead of ViewMap:**
```csharp
// Wrong
new ViewMap<DetailsPage, DetailsViewModel>()

// Correct
new DataViewMap<DetailsPage, DetailsViewModel, Product>()
```

2. **Check constructor parameter type:**
```csharp
public DetailsViewModel(Product product)  // Type must match DataViewMap<,,TData>
{
    Product = product ?? throw new ArgumentNullException(nameof(product));
}
```

3. **Verify data is passed in navigation:**
```csharp
await _navigator.NavigateDataAsync(this, data: myProduct);
// or
await _navigator.NavigateViewModelAsync<DetailsViewModel>(this, data: myProduct);
```

## XAML Navigation Issues

### Navigation.Request Not Triggering

**Symptoms:**
- Clicking button does nothing
- No navigation occurs

**Solutions:**

1. **Ensure button is inside region hierarchy:**
```xml
<Grid uen:Region.Attached="True">
    <Button uen:Navigation.Request="Products" />  <!-- Must be inside region -->
</Grid>
```

2. **Check for conflicting Click handler:**
```xml
<!-- Click handler may interfere -->
<Button Click="OnClick"
        uen:Navigation.Request="Products" />  <!-- May not work -->

<!-- Use only Navigation.Request -->
<Button uen:Navigation.Request="Products" />
```

3. **Verify route exists:**
   - Route name must be registered in RouteMap

### Navigation.Data Not Passed

**Symptoms:**
- Target ViewModel receives null data
- Binding to Navigation.Data returns null

**Solutions:**

1. **Check binding syntax:**
```xml
<Button uen:Navigation.Request="Details"
        uen:Navigation.Data="{Binding SelectedItem}" />
```

2. **Ensure binding source has value:**
   - `SelectedItem` must not be null when navigation occurs

3. **Verify DataViewMap registration:**
```csharp
new DataViewMap<DetailsPage, DetailsViewModel, ItemType>()
```

## Region Navigator Issues

### Visibility Navigation Not Switching

**Symptoms:**
- Content panels not switching
- All panels visible or none visible

**Solutions:**

1. **Set Region.Navigator on parent:**
```xml
<Grid uen:Region.Attached="True"
      uen:Region.Navigator="Visibility">  <!-- Required on parent -->
    <Grid uen:Region.Name="Tab1" Visibility="Collapsed" />
    <Grid uen:Region.Name="Tab2" Visibility="Collapsed" />
</Grid>
```

2. **Set initial Visibility:**
```xml
<Grid uen:Region.Name="Tab1" Visibility="Visible" />  <!-- One should be Visible -->
<Grid uen:Region.Name="Tab2" Visibility="Collapsed" />
```

3. **Check Region.Name values match navigation requests:**
```xml
<Button uen:Navigation.Request="Tab1" />  <!-- Must match Region.Name exactly -->
```

### Frame Navigation Back Not Working

**Symptoms:**
- Back button does nothing
- NavigateBack returns without effect

**Solutions:**

1. **Verify Frame has back stack:**
   - Navigate forward first before navigating back

2. **Use correct qualifier:**
```csharp
await _navigator.NavigateBackAsync(this);
// Not
await _navigator.NavigateRouteAsync(this, "-");  // Wrong
```

3. **Check Frame is in region hierarchy:**
```xml
<Grid uen:Region.Attached="True">
    <Frame uen:Region.Attached="True" />
</Grid>
```

## Dialog Navigation Issues

### Dialog Not Appearing

**Symptoms:**
- `ShowMessageDialogAsync` returns immediately
- No dialog visible

**Solutions:**

1. **Await the dialog call:**
```csharp
var result = await _navigator.ShowMessageDialogAsync(this, title: "Confirm", content: "Proceed?");
```

2. **Ensure navigation is configured:**
   - Dialog navigation requires proper Navigation Extensions setup

### Custom Dialog Not Opening

**Symptoms:**
- Page-as-dialog not displaying
- Navigation to dialog route fails

**Solutions:**

1. **Use Dialog qualifier:**
```csharp
await _navigator.NavigateViewAsync<MyDialogPage>(this, qualifier: Qualifiers.Dialog);
```

2. **In XAML, use ! prefix:**
```xml
<Button uen:Navigation.Request="!MyDialog" />
```

3. **Check dialog page is registered:**
```csharp
views.Register(
    new ViewMap<MyDialogPage, MyDialogViewModel>()
);
```

## Performance Issues

### Slow Navigation

**Symptoms:**
- Noticeable delay when navigating
- UI freezes during navigation

**Solutions:**

1. **Defer heavy loading:**
```csharp
public DetailsViewModel()
{
    // Don't do heavy work in constructor
    _ = LoadDataAsync();  // Fire and forget
}

private async Task LoadDataAsync()
{
    await Task.Delay(1);  // Yield to UI thread
    // Then load data
}
```

2. **Use async data loading:**
```csharp
// With MVUX
public IFeed<List<Item>> Items => Feed.Async(async ct => await _service.GetItemsAsync(ct));
```

3. **Lazy load nested content:**
   - Only load visible content initially
   - Load additional content on demand

### Memory Issues

**Symptoms:**
- Memory grows with navigation
- Views not being garbage collected

**Solutions:**

1. **Check for event handler leaks:**
```csharp
protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    base.OnNavigatedFrom(e);
    _someService.DataChanged -= OnDataChanged;  // Unsubscribe
}
```

2. **Dispose resources properly:**
```csharp
public void Dispose()
{
    _subscription?.Dispose();
    _cancellationTokenSource?.Cancel();
}
```

## Debugging Tips

### Enable Navigation Logging

Add logging configuration:

```csharp
.ConfigureServices(services =>
{
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Debug);
    });
})
```

### Inspect Region Hierarchy

Use Visual Studio's Live Visual Tree to:
1. Verify `Region.Attached` properties
2. Check `Region.Name` values
3. Confirm visibility states

### Check Route Resolution

Add breakpoint in route registration to verify:
- All views are registered
- Routes have correct paths
- Nested routes are properly configured

## Quick Reference: Error Solutions

| Error | Likely Cause | Solution |
|-------|--------------|----------|
| Route not found | Spelling mismatch | Check route name case-sensitivity |
| ViewModel null | Missing DI registration | Register ViewModel in services |
| Data not passed | Wrong ViewMap type | Use DataViewMap |
| Region not working | Missing Region.Attached | Add to parent elements |
| Dialog not showing | Not awaited | Use await with ShowMessageDialogAsync |
| Back navigation fails | Empty back stack | Verify previous navigation occurred |
| Visibility not switching | Missing Region.Navigator | Add `Region.Navigator="Visibility"` to parent |
| TabBar not selecting | Region.Name mismatch | Verify names match routes |
| ContentControl empty | Alignment not set | Set HorizontalContentAlignment="Stretch" |
