# Uno.Extensions.Telemetry — Specification

**Status:** Draft
**Date:** 2026-03-04
**Author:** Steve Bilogan

---

## 1. Overview

Add first-class telemetry support to Uno.Extensions, enabling applications to collect **traces**, **metrics**, and **crash/error reports** through a unified API surface. The design follows the established `UseXxx` / `IXxxBuilder` pattern used by Authentication, Navigation, and other Uno.Extensions features.

The extension should:
- Build on top of **OpenTelemetry .NET** as the core instrumentation layer.
- Provide **auto-wiring** packages for popular exporters (Sentry, Azure Monitor / Application Insights, Raygun) so that developers can enable telemetry with a single `Add*()` call plus a connection string.
- Integrate naturally with the existing `Uno.Extensions.Logging` and `Uno.Extensions.Http` infrastructure.
- Work across all Uno Platform targets (iOS, Android, macOS Catalyst, Windows, WASM, Desktop/Skia).

---

## 2. Goals & Non-Goals

### Goals
| # | Goal |
|---|------|
| G1 | Unified `UseTelemetry()` entry point on `IHostBuilder` |
| G2 | Sub-builder pattern (`ITelemetryBuilder`) for composing providers |
| G3 | OpenTelemetry-based traces and metrics out of the box |
| G4 | Auto-wiring exporter packages: Sentry, Azure Monitor, Raygun, Application Insights |
| G5 | Unhandled exception / crash reporting wired automatically |
| G6 | HTTP request instrumentation auto-wired when `Uno.Extensions.Http` is present |
| G7 | Configuration-driven setup via `appsettings.json` |
| G8 | Cross-platform: all Uno target frameworks |

### Non-Goals
| # | Non-Goal |
|---|----------|
| NG1 | Building a custom telemetry pipeline — we delegate to OpenTelemetry |
| NG2 | UI-level analytics (screen view tracking, button tap tracking) — may come later |
| NG3 | Backend/server-side telemetry infrastructure |
| NG4 | Real-time dashboarding — that is the exporter/backend's responsibility |

---

## 3. Package Structure

```
Uno.Extensions.Telemetry                          # Core: ITelemetryBuilder, UseTelemetry, config, crash hooks
Uno.Extensions.Telemetry.OpenTelemetry            # AddOpenTelemetry(): OTLP exporter, tracing & metrics builders
Uno.Extensions.Telemetry.Sentry                   # AddSentry(): auto-wires Sentry SDK + OTel exporter
Uno.Extensions.Telemetry.AzureMonitor             # AddAzureMonitor(): auto-wires Azure Monitor OTel exporter
Uno.Extensions.Telemetry.ApplicationInsights      # AddApplicationInsights(): auto-wires classic AppInsights SDK
Uno.Extensions.Telemetry.Raygun                   # AddRaygun(): auto-wires Raygun crash reporting + OTel exporter
```

Each exporter package references the core `Uno.Extensions.Telemetry` package and extends `ITelemetryBuilder` with an `AddXxx()` method.

---

## 4. Public API Surface

### 4.1 Core — `Uno.Extensions.Telemetry`

```csharp
// Namespace: Uno.Extensions (follows convention)
namespace Uno.Extensions;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Registers telemetry services and invokes the builder for provider configuration.
    /// </summary>
    public static IHostBuilder UseTelemetry(
        this IHostBuilder hostBuilder,
        Action<ITelemetryBuilder> configure);

    public static IHostBuilder UseTelemetry(
        this IHostBuilder hostBuilder,
        Action<HostBuilderContext, ITelemetryBuilder>? configure = default);
}
```

```csharp
namespace Uno.Extensions.Telemetry;

/// <summary>
/// Fluent builder for composing telemetry providers.
/// </summary>
public interface ITelemetryBuilder
{
    // Marker interface — provider packages add extension methods here.
}
```

```csharp
namespace Uno.Extensions.Telemetry;

/// <summary>
/// Configuration POCO bound from appsettings.json section "Telemetry".
/// </summary>
public record TelemetryConfiguration
{
    /// <summary>Master kill-switch.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Logical service name attached to all telemetry signals.</summary>
    public string? ServiceName { get; init; }

    /// <summary>Service version attached to all telemetry signals.</summary>
    public string? ServiceVersion { get; init; }

    /// <summary>Enable automatic unhandled exception capture.</summary>
    public bool CaptureUnhandledExceptions { get; init; } = true;

    /// <summary>Provider-specific configuration sections.</summary>
    public SentryTelemetryOptions? Sentry { get; init; }
    public AzureMonitorOptions? AzureMonitor { get; init; }
    public ApplicationInsightsOptions? ApplicationInsights { get; init; }
    public RaygunOptions? Raygun { get; init; }
    public OpenTelemetryOptions? OpenTelemetry { get; init; }
}
```

```csharp
namespace Uno.Extensions.Telemetry;

/// <summary>
/// Runtime service for manual telemetry operations.
/// </summary>
public interface ITelemetryService
{
    /// <summary>Reports an exception manually.</summary>
    void ReportError(Exception exception, IDictionary<string, string>? properties = null);

    /// <summary>Reports a custom event / breadcrumb.</summary>
    void TrackEvent(string eventName, IDictionary<string, string>? properties = null);

    /// <summary>Whether telemetry is currently enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Allows the user to opt-in / opt-out at runtime.</summary>
    void SetEnabled(bool enabled);
}
```

### 4.2 OpenTelemetry Exporter — `Uno.Extensions.Telemetry.OpenTelemetry`

```csharp
namespace Uno.Extensions;

public static class TelemetryBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing and metrics with the OTLP exporter.
    /// Connection string / endpoint can be set in appsettings.json under Telemetry:OpenTelemetry.
    /// </summary>
    public static ITelemetryBuilder AddOpenTelemetry(
        this ITelemetryBuilder builder,
        Action<OpenTelemetryOptions>? configure = null);
}

public record OpenTelemetryOptions
{
    /// <summary>Callback to customize the TracerProviderBuilder.</summary>
    public Action<TracerProviderBuilder>? ConfigureTracing { get; init; }

    /// <summary>Callback to customize the MeterProviderBuilder.</summary>
    public Action<MeterProviderBuilder>? ConfigureMetrics { get; init; }

    /// <summary>OTLP endpoint. Falls back to Telemetry:OpenTelemetry:Endpoint in config.</summary>
    public string? Endpoint { get; init; }
}
```

### 4.3 Sentry — `Uno.Extensions.Telemetry.Sentry`

```csharp
namespace Uno.Extensions;

public static class TelemetryBuilderExtensions
{
    /// <summary>
    /// Auto-wires the Sentry SDK for crash reporting and the Sentry OTel exporter for traces.
    /// DSN is read from Telemetry:Sentry:Dsn in appsettings.json or can be set via options.
    /// </summary>
    public static ITelemetryBuilder AddSentry(
        this ITelemetryBuilder builder,
        Action<SentryTelemetryOptions>? configure = null);
}

public record SentryTelemetryOptions
{
    public string? Dsn { get; init; }
    public double TracesSampleRate { get; init; } = 1.0;
    public bool CaptureFailedRequests { get; init; } = true;
    public string? Environment { get; init; }
}
```

### 4.4 Azure Monitor — `Uno.Extensions.Telemetry.AzureMonitor`

```csharp
namespace Uno.Extensions;

public static class TelemetryBuilderExtensions
{
    /// <summary>
    /// Auto-wires the Azure Monitor OpenTelemetry exporter.
    /// Connection string from Telemetry:AzureMonitor:ConnectionString in appsettings.json.
    /// </summary>
    public static ITelemetryBuilder AddAzureMonitor(
        this ITelemetryBuilder builder,
        Action<AzureMonitorOptions>? configure = null);
}

public record AzureMonitorOptions
{
    public string? ConnectionString { get; init; }
}
```

### 4.5 Application Insights — `Uno.Extensions.Telemetry.ApplicationInsights`

```csharp
namespace Uno.Extensions;

public static class TelemetryBuilderExtensions
{
    /// <summary>
    /// Auto-wires the Application Insights SDK (classic, non-OTel path).
    /// Connection string from Telemetry:ApplicationInsights:ConnectionString.
    /// </summary>
    public static ITelemetryBuilder AddApplicationInsights(
        this ITelemetryBuilder builder,
        Action<ApplicationInsightsOptions>? configure = null);
}

public record ApplicationInsightsOptions
{
    public string? ConnectionString { get; init; }
    public string? InstrumentationKey { get; init; }
}
```

### 4.6 Raygun — `Uno.Extensions.Telemetry.Raygun`

```csharp
namespace Uno.Extensions;

public static class TelemetryBuilderExtensions
{
    /// <summary>
    /// Auto-wires Raygun crash reporting.
    /// API key from Telemetry:Raygun:ApiKey in appsettings.json.
    /// </summary>
    public static ITelemetryBuilder AddRaygun(
        this ITelemetryBuilder builder,
        Action<RaygunOptions>? configure = null);
}

public record RaygunOptions
{
    public string? ApiKey { get; init; }
    public bool CaptureUnhandledExceptions { get; init; } = true;
}
```

---

## 5. Configuration via `appsettings.json`

All provider settings can be driven from configuration, minimizing code:

```json
{
  "Telemetry": {
    "IsEnabled": true,
    "ServiceName": "MyApp",
    "ServiceVersion": "1.0.0",
    "CaptureUnhandledExceptions": true,
    "OpenTelemetry": {
      "Endpoint": "http://localhost:4317"
    },
    "Sentry": {
      "Dsn": "https://examplePublicKey@o0.ingest.sentry.io/0",
      "TracesSampleRate": 0.5,
      "Environment": "production"
    },
    "AzureMonitor": {
      "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=..."
    },
    "ApplicationInsights": {
      "ConnectionString": "InstrumentationKey=..."
    },
    "Raygun": {
      "ApiKey": "paste-your-api-key"
    }
  }
}
```

Provider `AddXxx()` methods merge code-based options with config-based options (code wins).

---

## 6. Auto-Wiring Behavior

Each `AddXxx()` method performs the following automatically:

| Step | What happens |
|------|-------------|
| 1. Read config | Bind the provider-specific section from `IConfiguration` |
| 2. Register SDK | Add the provider's SDK services to DI |
| 3. Wire OTel exporter | If the provider supports OpenTelemetry, register its `SpanExporter` / `MetricReader` |
| 4. Wire crash handler | Register an `IServiceInitialize` that hooks `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`, and platform-specific crash hooks |
| 5. Wire HTTP instrumentation | If `Uno.Extensions.Http` is registered (detected via `IsRegistered`), add `HttpClient` instrumentation to tracing |
| 6. Wire logging bridge | Connect `ILoggerProvider` from the provider (e.g., Sentry's log integration) to `Microsoft.Extensions.Logging` |

### Platform-Specific Crash Hooks

| Platform | Hook |
|----------|------|
| iOS / macOS Catalyst | `NSSetUncaughtExceptionHandler` via ObjC interop, `AppDomain.UnhandledException` |
| Android | `AndroidEnvironment.UnhandledExceptionRaiser`, `Java.Lang.Thread.DefaultUncaughtExceptionHandler` |
| Windows (WinUI) | `Application.UnhandledException`, `AppDomain.UnhandledException` |
| WASM | `AppDomain.UnhandledException`, JS `window.onerror` / `unhandledrejection` interop |
| Desktop (Skia) | `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException` |

---

## 7. Integration with Existing Extensions

### 7.1 `Uno.Extensions.Http`

When both `UseTelemetry()` and `UseHttp()` are registered:

- Automatically add an OpenTelemetry `HttpClientInstrumentation` source to the tracing pipeline.
- Optionally replace or augment the existing `DiagnosticHandler` with a telemetry-aware `DelegatingHandler` that creates spans per HTTP request with attributes: `http.method`, `http.url`, `http.status_code`, `http.request.duration`.

### 7.2 `Uno.Extensions.Logging`

- Register an OpenTelemetry `ILoggerProvider` so that log messages are correlated with active traces (trace ID / span ID injected into log scopes).
- Sentry and Raygun packages additionally register their own `ILoggerProvider` for breadcrumb capture.

### 7.3 `Uno.Extensions.Navigation`

- (Future) Optionally emit navigation spans: `navigation.route`, `navigation.duration`, `navigation.result`.

---

## 8. Usage Examples

### 8.1 Minimal — Sentry Only

```csharp
var host = UnoHost.CreateDefaultBuilder()
    .UseTelemetry(t => t.AddSentry())
    .Build();
```

With `appsettings.json`:
```json
{ "Telemetry": { "Sentry": { "Dsn": "https://key@sentry.io/123" } } }
```

### 8.2 Full — Multiple Providers

```csharp
var host = UnoHost.CreateDefaultBuilder()
    .UseConfiguration(configure: cb => cb.EmbeddedSource<App>())
    .UseLogging()
    .UseHttp()
    .UseTelemetry(t => t
        .AddOpenTelemetry(otel =>
        {
            otel.ConfigureTracing = tracing => tracing
                .AddSource("MyApp.*");
            otel.ConfigureMetrics = metrics => metrics
                .AddMeter("MyApp.Orders");
        })
        .AddSentry()
        .AddAzureMonitor())
    .UseNavigation(...)
    .Build();
```

### 8.3 Manual Event Tracking

```csharp
public class OrderViewModel
{
    private readonly ITelemetryService _telemetry;

    public OrderViewModel(ITelemetryService telemetry)
    {
        _telemetry = telemetry;
    }

    public async Task PlaceOrder(Order order)
    {
        try
        {
            await _orderService.Submit(order);
            _telemetry.TrackEvent("OrderPlaced", new Dictionary<string, string>
            {
                ["OrderId"] = order.Id,
                ["ItemCount"] = order.Items.Count.ToString()
            });
        }
        catch (Exception ex)
        {
            _telemetry.ReportError(ex, new Dictionary<string, string>
            {
                ["OrderId"] = order.Id
            });
            throw;
        }
    }
}
```

### 8.4 Runtime Opt-Out (GDPR / Privacy)

```csharp
// In a settings page
public void OnTelemetryConsentChanged(bool consented)
{
    var telemetry = _host.Services.GetRequiredService<ITelemetryService>();
    telemetry.SetEnabled(consented);
}
```

---

## 9. Implementation Plan

### Phase 1 — Core + OpenTelemetry (MVP)
1. Create `Uno.Extensions.Telemetry` project with `ITelemetryBuilder`, `TelemetryConfiguration`, `ITelemetryService`, `UseTelemetry()`.
2. Create `Uno.Extensions.Telemetry.OpenTelemetry` with `AddOpenTelemetry()` wiring OTLP exporter, basic tracing and metrics.
3. Implement crash hook infrastructure per platform.
4. Wire HTTP instrumentation when `Uno.Extensions.Http` is present.

### Phase 2 — Exporter Packages
5. `Uno.Extensions.Telemetry.Sentry` — Sentry SDK + OTel span exporter + crash reporting.
6. `Uno.Extensions.Telemetry.AzureMonitor` — Azure Monitor OTel exporter.
7. `Uno.Extensions.Telemetry.ApplicationInsights` — Classic AppInsights SDK.
8. `Uno.Extensions.Telemetry.Raygun` — Raygun4Net + crash reporting.

### Phase 3 — Polish
9. Logging bridge (OTel logs, Sentry breadcrumbs).
10. Navigation span instrumentation (opt-in).
11. Documentation and samples.
12. Unit and integration tests.

---

## 10. NuGet Dependencies (Expected)

| Package | Dependency |
|---------|-----------|
| `Uno.Extensions.Telemetry` | `Uno.Extensions.Hosting`, `Uno.Extensions.Configuration`, `Microsoft.Extensions.Logging.Abstractions` |
| `Uno.Extensions.Telemetry.OpenTelemetry` | `OpenTelemetry`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.Http` |
| `Uno.Extensions.Telemetry.Sentry` | `Sentry`, `Sentry.OpenTelemetry` |
| `Uno.Extensions.Telemetry.AzureMonitor` | `Azure.Monitor.OpenTelemetry.Exporter` |
| `Uno.Extensions.Telemetry.ApplicationInsights` | `Microsoft.ApplicationInsights` |
| `Uno.Extensions.Telemetry.Raygun` | `Mindscape.Raygun4Net` |

---

## 11. Open Questions

| # | Question | Notes |
|---|----------|-------|
| Q1 | Should `ITelemetryService` also expose a `StartSpan()` / `StartActivity()` API for manual tracing, or rely on `System.Diagnostics.ActivitySource` directly? | Leaning toward thin wrapper for discoverability. |
| Q2 | Should the core package include a no-op / console exporter for local dev, or require an explicit exporter? | A console exporter in `#if DEBUG` would ease onboarding. |
| Q3 | Should we support **multiple exporters simultaneously** (e.g., Sentry + Azure Monitor)? | Yes — OpenTelemetry supports composite exporters natively. |
| Q4 | Do we need a `Uno.Extensions.Telemetry.WinUI` split, or can the core package handle all platforms? | Platform crash hooks may require the WinUI split for `Application.UnhandledException` access. |
| Q5 | Should navigation span instrumentation be in core or a separate `Uno.Extensions.Telemetry.Navigation` package? | Separate to avoid circular dependency with Navigation. |
