---
name: mvux-overview
description: Understand MVUX (Model-View-Update-eXtended) architecture in Uno Platform. Use when learning about MVUX patterns, comparing MVUX to MVVM, or understanding immutable data flow with reactive feeds and states.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# MVUX Overview - Model-View-Update-eXtended

MVUX is an implementation of the Model-View-Update pattern designed for Uno Platform that combines unidirectional data flow with XAML data binding support.

## Prerequisites

- Uno Platform 5.x or later
- .NET 8.0 or later
- Add `MVUX` to `<UnoFeatures>` in your `.csproj`

```xml
<UnoFeatures>MVUX</UnoFeatures>
```

## Core Concepts

### What is MVUX?

MVUX stands for **M**odel, **V**iew, **U**pdate, e**X**tended. It differs from other MVU implementations by supporting data binding while maintaining immutable data flow.

### Key Differences from MVVM

| Aspect | MVVM | MVUX |
|--------|------|------|
| Data Flow | Bidirectional | Unidirectional |
| Model | Mutable | Immutable (records) |
| State Management | Manual INotifyPropertyChanged | Automatic via Feeds/States |
| Async Handling | Manual loading/error states | Built-in via IFeed/IState |

## MVUX Data Flow

```
┌─────────┐     Data Binding    ┌───────────┐
│  View   │◄──────────────────►│ ViewModel │
└─────────┘                     └───────────┘
                                      │
                                      ▼
                                ┌───────────┐
                                │  Update   │
                                └───────────┘
                                      │
                                      ▼
                                ┌───────────┐
                                │   Model   │
                                └───────────┘
                                      │
                                      ▼
                              (New Model Instance)
                                      │
                                      ▼
                                ┌───────────┐
                                │ ViewModel │
                                └───────────┘
```

## Basic Example

### Model Definition

```csharp
using Uno.Extensions.Reactive;

// Models should be records (immutable)
public record WeatherInfo(double Temperature, string Description);

// Model class with *Model suffix triggers code generation
public partial record MainModel(IWeatherService WeatherService)
{
    // IFeed represents async data with loading/error states
    public IFeed<WeatherInfo> CurrentWeather => 
        Feed.Async(WeatherService.GetCurrentWeather);
}
```

### Generated ViewModel

MVUX generates a `MainViewModel` from `MainModel` that:
- Exposes `CurrentWeather` as a bindable property
- Handles loading/error states automatically
- Supports data binding

### View (XAML)

```xml
<Page x:Class="MyApp.MainPage"
      xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    
    <mvux:FeedView Source="{Binding CurrentWeather}">
        <mvux:FeedView.ValueTemplate>
            <DataTemplate>
                <TextBlock Text="{Binding Data.Temperature}" />
            </DataTemplate>
        </mvux:FeedView.ValueTemplate>
        <mvux:FeedView.ProgressTemplate>
            <DataTemplate>
                <ProgressRing IsActive="True" />
            </DataTemplate>
        </mvux:FeedView.ProgressTemplate>
        <mvux:FeedView.ErrorTemplate>
            <DataTemplate>
                <TextBlock Text="Error loading data" />
            </DataTemplate>
        </mvux:FeedView.ErrorTemplate>
    </mvux:FeedView>
</Page>
```

### Code-Behind

```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        // Use the generated ViewModel, not the Model directly
        DataContext = new MainViewModel(new WeatherService());
    }
}
```

## Core Types

### IFeed<T>
- Read-only reactive stream of values
- Includes loading/error/none states
- Created via `Feed.Async()`, `Feed.AsyncEnumerable()`

### IState<T>
- Mutable reactive value
- Supports two-way binding
- Created via `State.Value()`, `State.Async()`

### IListFeed<T>
- Read-only reactive collection
- Created via `ListFeed.Async()`

### IListState<T>
- Mutable reactive collection
- Supports add/remove/update operations

## Best Practices

1. **Use records** for Models and entities (immutable by default)
2. **Suffix Model classes** with `Model` (e.g., `MainModel`) for code generation
3. **Mark Model classes as partial** to allow generated code
4. **Use FeedView** to handle all async states (loading, error, none, value)
5. **Inject services** via constructor parameters in records
6. **Use CancellationToken** in async methods for proper cancellation

## When to Use MVUX

Use MVUX when:
- You want automatic handling of loading/error states
- You prefer immutable data models
- You want simpler async data binding
- You need reactive data flow

Use MVVM when:
- You have existing MVVM infrastructure
- You need fine-grained control over property changes
- Your team is more familiar with traditional MVVM
