---
name: mvux-feedview
description: Display async feed data with FeedView control. Use when rendering IFeed/IState data with automatic loading, error, empty, and value state handling in XAML.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# FeedView Control

The `FeedView` control is the primary way to consume and display `IFeed<T>` and `IState<T>` data in MVUX, automatically handling all async states.

## Prerequisites

- Add `MVUX` to `<UnoFeatures>` in your `.csproj`
- Add XAML namespace: `xmlns:mvux="using:Uno.Extensions.Reactive.UI"`

## Basic Usage

```xml
<Page xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    
    <mvux:FeedView Source="{Binding MyFeed}">
        <DataTemplate>
            <TextBlock Text="{Binding Data.Name}" />
        </DataTemplate>
    </mvux:FeedView>
</Page>
```

## FeedView Templates

### ValueTemplate (Default Content)

The main template for displaying data when available:

```xml
<mvux:FeedView Source="{Binding Product}">
    <mvux:FeedView.ValueTemplate>
        <DataTemplate>
            <StackPanel>
                <TextBlock Text="{Binding Data.Name}" FontSize="20" />
                <TextBlock Text="{Binding Data.Price}" />
            </StackPanel>
        </DataTemplate>
    </mvux:FeedView.ValueTemplate>
</mvux:FeedView>
```

> **Note:** Content placed directly inside `<FeedView>` is automatically treated as `ValueTemplate`.

### ProgressTemplate (Loading State)

Displayed while the feed is loading:

```xml
<mvux:FeedView Source="{Binding Product}">
    <DataTemplate>
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>
    
    <mvux:FeedView.ProgressTemplate>
        <DataTemplate>
            <StackPanel HorizontalAlignment="Center">
                <ProgressRing IsActive="True" />
                <TextBlock Text="Loading..." Margin="0,8,0,0" />
            </StackPanel>
        </DataTemplate>
    </mvux:FeedView.ProgressTemplate>
</mvux:FeedView>
```

### ErrorTemplate (Error State)

Displayed when the feed encounters an error:

```xml
<mvux:FeedView Source="{Binding Product}">
    <DataTemplate>
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>
    
    <mvux:FeedView.ErrorTemplate>
        <DataTemplate>
            <StackPanel Spacing="8">
                <TextBlock Text="Something went wrong" />
                <TextBlock Text="{Binding Error.Message}" 
                           Opacity="0.6" 
                           TextWrapping="Wrap" />
                <Button Content="Retry" Command="{Binding Refresh}" />
            </StackPanel>
        </DataTemplate>
    </mvux:FeedView.ErrorTemplate>
</mvux:FeedView>
```

### NoneTemplate (Empty/Null State)

Displayed when the feed completes successfully but returns no data:

```xml
<mvux:FeedView Source="{Binding SearchResults}">
    <DataTemplate>
        <ListView ItemsSource="{Binding Data}" />
    </DataTemplate>
    
    <mvux:FeedView.NoneTemplate>
        <DataTemplate>
            <TextBlock Text="No results found" 
                       HorizontalAlignment="Center" />
        </DataTemplate>
    </mvux:FeedView.NoneTemplate>
</mvux:FeedView>
```

### UndefinedTemplate (Initial State)

Displayed briefly before the feed starts loading:

```xml
<mvux:FeedView Source="{Binding Product}">
    <DataTemplate>
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>
    
    <mvux:FeedView.UndefinedTemplate>
        <DataTemplate>
            <TextBlock Text="Initializing..." />
        </DataTemplate>
    </mvux:FeedView.UndefinedTemplate>
</mvux:FeedView>
```

## FeedViewState Properties

Inside FeedView templates, the DataContext is a `FeedViewState` with these properties:

| Property | Type | Description |
|----------|------|-------------|
| `Data` | `T` | The current value from the feed |
| `Refresh` | `ICommand` | Command to refresh the feed |
| `Progress` | `bool` | True if loading/refreshing |
| `Error` | `Exception` | The error if one occurred |
| `Parent` | `object` | The original DataContext (page ViewModel) |

### Accessing Data

```xml
<mvux:FeedView Source="{Binding Product}">
    <DataTemplate>
        <!-- Data is the feed value -->
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>
</mvux:FeedView>
```

### Refresh Command

```xml
<mvux:FeedView Source="{Binding Product}">
    <DataTemplate>
        <StackPanel>
            <TextBlock Text="{Binding Data.Name}" />
            <Button Content="Refresh" Command="{Binding Refresh}" />
        </StackPanel>
    </DataTemplate>
</mvux:FeedView>
```

### Accessing Parent ViewModel

```xml
<mvux:FeedView Source="{Binding Product}">
    <DataTemplate>
        <StackPanel>
            <TextBlock Text="{Binding Data.Name}" />
            <!-- Access page ViewModel command -->
            <Button Content="Edit" 
                    Command="{Binding Parent.EditProduct}"
                    CommandParameter="{Binding Data}" />
        </StackPanel>
    </DataTemplate>
</mvux:FeedView>
```

## Refresh from Outside FeedView

```xml
<Grid>
    <mvux:FeedView x:Name="ProductFeed" Source="{Binding Product}">
        <DataTemplate>
            <TextBlock Text="{Binding Data.Name}" />
        </DataTemplate>
    </mvux:FeedView>
    
    <!-- Button outside FeedView -->
    <Button Content="Refresh" 
            Command="{Binding Refresh, ElementName=ProductFeed}"
            VerticalAlignment="Top"
            HorizontalAlignment="Right" />
</Grid>
```

## RefreshingState Property

Control the loading behavior during refresh:

```xml
<!-- Don't show progress during refresh -->
<mvux:FeedView Source="{Binding Product}" 
               RefreshingState="None">
    <DataTemplate>
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>
</mvux:FeedView>
```

Values:
- `None` - Don't show loading UI during refresh
- `Default` / `Loading` - Show loading UI (default)

## List Data with FeedView

```xml
<mvux:FeedView Source="{Binding Products}">
    <DataTemplate>
        <ListView ItemsSource="{Binding Data}">
            <ListView.Header>
                <Button Content="Refresh" Command="{Binding Refresh}" />
            </ListView.Header>
            <ListView.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Name}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </DataTemplate>
</mvux:FeedView>
```

## Complete Example

```xml
<Page x:Class="MyApp.ProductPage"
      xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    
    <mvux:FeedView Source="{Binding Product}">
        <!-- Value Template -->
        <mvux:FeedView.ValueTemplate>
            <DataTemplate>
                <StackPanel Spacing="8" Padding="16">
                    <TextBlock Text="{Binding Data.Name}" 
                               Style="{StaticResource TitleTextBlockStyle}" />
                    <TextBlock Text="{Binding Data.Description}" />
                    <TextBlock Text="{Binding Data.Price}" 
                               Style="{StaticResource SubtitleTextBlockStyle}" />
                    <Button Content="Refresh" Command="{Binding Refresh}" />
                </StackPanel>
            </DataTemplate>
        </mvux:FeedView.ValueTemplate>
        
        <!-- Loading Template -->
        <mvux:FeedView.ProgressTemplate>
            <DataTemplate>
                <Grid>
                    <ProgressRing IsActive="True" />
                </Grid>
            </DataTemplate>
        </mvux:FeedView.ProgressTemplate>
        
        <!-- Error Template -->
        <mvux:FeedView.ErrorTemplate>
            <DataTemplate>
                <StackPanel Spacing="8" Padding="16" 
                            HorizontalAlignment="Center">
                    <SymbolIcon Symbol="Warning" />
                    <TextBlock Text="Failed to load product" />
                    <Button Content="Try Again" Command="{Binding Refresh}" />
                </StackPanel>
            </DataTemplate>
        </mvux:FeedView.ErrorTemplate>
        
        <!-- Empty Template -->
        <mvux:FeedView.NoneTemplate>
            <DataTemplate>
                <TextBlock Text="Product not found" 
                           HorizontalAlignment="Center" />
            </DataTemplate>
        </mvux:FeedView.NoneTemplate>
    </mvux:FeedView>
</Page>
```

## Best Practices

1. **Always define ErrorTemplate** with a Refresh button for retry
2. **Use NoneTemplate** for empty/null states vs. error states
3. **Access parent ViewModel** via `Parent` property when needed
4. **Use ElementName binding** for Refresh from outside FeedView
5. **Set RefreshingState="None"** when you want seamless background refresh
6. **Use Data property** to access the actual feed value in templates
