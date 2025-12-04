---
uid: Uno.Extensions.Http.Http.HowTo
title: Centralize HTTP Endpoints
tags: [http, configuration, typed-client]
---

> **UnoFeatures:** `Http` (add to `<UnoFeatures>` in your `.csproj`)

# Centralize HTTP endpoints from appsettings

Drive Uno Platform HTTP clients from configuration so each service shares resilient, named `HttpClient` instances.

## Enable shared HTTP clients

Add the `Http` UnoFeature to the shared project so `Uno.Extensions.Http.WinUI` is included.

```diff
<UnoFeatures>
    Material;
    Extensions;
+   Http;
    Toolkit;
    MVUX;
</UnoFeatures>
```

> [!NOTE]
> As of Uno Platform 6.0, the `Http` feature no longer bundles Refit or Kiota clients. Use the dedicated `HttpRefit` or `HttpKiota` features when you need those integrations. See [Migrating to Uno Platform 6.0](xref:Uno.Development.MigratingToUno6).

## Register a typed endpoint

Register the HTTP feature on the host and map your service to a configuration section.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(hostBuilder =>
        {
            hostBuilder.UseHttp((context, services) =>
            {
                services.AddClient<IShowService, ShowService>(context, "ShowService");
            });
        });
}

public interface IShowService
{
    Task<Show> GetShowAsync();
}
```

`AddClient` wires up dependency injection so the `ShowService` gets a named `HttpClient`.

## Configure the endpoint in appsettings

Create or update `appsettings.json` with the endpoint configuration.

```json
{
  "ShowService": {
    "Url": "https://ch9-app.azurewebsites.net/",
    "UseNativeHandler": true
  }
}
```

Put the section name in the `AddClient` call to connect configuration with the typed client.

## Call the service from your view model

Inject the typed service and let it consume the configured client.

```csharp
public class ShowViewModel : ObservableObject
{
    private readonly IShowService _showService;

    public ShowViewModel(IShowService showService) => _showService = showService;

    public async Task LoadShowAsync()
    {
        var show = await _showService.GetShowAsync();
        // update state or UI with show
    }
}
```

The registered service receives a preconfigured `HttpClient`, letting you focus on domain logic.

## Resources

- [Consume a web API with HttpClient](xref:Uno.Development.ConsumeWebApi)
- [Create a strongly-typed REST client with Refit](xref:Uno.Extensions.Http.HowToRefit)
- [Configure HTTP with custom endpoint options](xref:Uno.Extensions.Http.HowToEndpointOptions)
- [HTTP overview](xref:Uno.Extensions.Http.Overview)
- [Polly and HttpClientFactory](https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory)
- [TestHarness HTTP endpoints](https://github.com/unoplatform/uno.extensions/tree/main/testing/TestHarness/TestHarness/Ext/Http/Endpoints/)
