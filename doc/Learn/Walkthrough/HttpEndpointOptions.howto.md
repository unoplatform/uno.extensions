---
uid: Uno.Extensions.Http.HttpEndpointOptions
title: Customize Endpoint Options
tags: [http, configuration, headers, endpoint-options]
---
# Inject custom headers with endpoint options

Capture API-specific settings in a custom options type and apply them each time Uno Extensions builds the `HttpClient`.

## Extend the endpoint options

Derive from `EndpointOptions` to add the extra values you want to surface in configuration.

```csharp
public class CustomEndpointOptions : EndpointOptions
{
    public string? ApiKey { get; set; }
}
```

Store per-endpoint details—like API keys or tenant identifiers—so they can be consumed when the client is constructed.

## Enable HTTP endpoints

Add the base `Http` feature to bring in `Uno.Extensions.Http.WinUI`.

```diff
<UnoFeatures>
    Material;
    Extensions;
+   Http;
    Toolkit;
    MVUX;
</UnoFeatures>
```

Register the feature on the host and map the endpoint name to the custom options.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(hostBuilder =>
        {
            hostBuilder.UseHttp((context, services) =>
            {
                services.AddClientWithEndpoint<HttpEndpointsOneViewModel, CustomEndpointOptions>(
                    context,
                    name: "HttpDummyJsonEndpoint",
                    configure: (builder, options) =>
                    {
                        builder.ConfigureHttpClient(client =>
                        {
                            if (!string.IsNullOrWhiteSpace(options?.ApiKey))
                            {
                                client.DefaultRequestHeaders.Add("ApiKey", options.ApiKey);
                            }
                        });
                    });
            });
        });
}
```

`AddClientWithEndpoint` passes the resolved `CustomEndpointOptions` instance into the `configure` callback.

## Configure the endpoint

Populate the configuration section so your options have real values.

```json
{
  "HttpDummyJsonEndpoint": {
    "Url": "https://dummyjson.com",
    "UseNativeHandler": true,
    "ApiKey": "FakeApiKey"
  }
}
```

The section name must match the `name` argument used during registration.

## Consume the configured client

Request the typed client—in this case `HttpEndpointsOneViewModel`—and rely on the injected `HttpClient`.

```csharp
public class HttpEndpointsOneViewModel
{
    private readonly HttpClient _client;

    public HttpEndpointsOneViewModel(HttpClient client) => _client = client;

    public async Task<string> LoadAsync(CancellationToken ct) =>
        await _client.GetStringAsync("products", ct);
}
```

Every call now includes the configured headers without additional boilerplate in your view model.

## Resources

- [Register HTTP endpoints](xref:Uno.Extensions.Http.HowToHttp)
- [Create a Refit client](xref:Uno.Extensions.Http.HowToRefit)
- [HTTP overview](xref:Uno.Extensions.Http.Overview)
- [Polly and HttpClientFactory](https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory)
- [TestHarness HTTP endpoints](https://github.com/unoplatform/uno.extensions/tree/main/testing/TestHarness/TestHarness/Ext/Http/Endpoints/)
