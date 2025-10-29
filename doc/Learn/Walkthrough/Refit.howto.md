---
uid: Uno.Extensions.Http.Refit
title: Build Refit Clients
tags: [http, refit, typed-client, configuration]
---
# Build Refit endpoints from configuration

Describe your REST API with a Refit interface, register it once, and let Uno Extensions provide a configured `HttpClient`.

## Enable Refit support

Add the `HttpRefit` feature so the project references `Uno.Extensions.Http.Refit`.

```diff
<UnoFeatures>
    Material;
    Extensions;
+   HttpRefit;
    Toolkit;
    MVUX;
</UnoFeatures>
```

The feature layers the standard HTTP infrastructure with Refit integration.

## Describe the API contract

Create an interface that maps HTTP verbs and routes to strongly typed calls.

```csharp
using Refit;

[Headers("Content-Type: application/json")]
public interface IChuckNorrisEndpoint
{
    [Get("/jokes/search")]
    Task<ApiResponse<ChuckNorrisData>> SearchAsync(
        CancellationToken ct,
        [AliasAs("query")] string term);
}

public sealed record ChuckNorrisData(long Total, IReadOnlyList<ChuckNorrisFact> Result);

public sealed record ChuckNorrisFact(string Value, string Id);
```

`AliasAs` forwards the method argument as the `query` string parameter on the GET request.

## Register the Refit client

Bind the interface to the host so Uno Extensions creates and reuses the Refit proxy.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(hostBuilder =>
        {
            hostBuilder.UseHttp((context, services) =>
            {
                services.AddRefitClient<IChuckNorrisEndpoint>(context);
            });
        });
}
```

`AddRefitClient` wires up `IChuckNorrisEndpoint` to an `HttpClientFactory` instance that is configurable through app settings.

## Configure the endpoint

Add a section to `appsettings.json` so the registered client gets its base address.

```json
{
  "ChuckNorrisEndpoint": {
    "Url": "https://api.chucknorris.io/",
    "UseNativeHandler": true
  }
}
```

Set `UseNativeHandler` to `false` when you need a managed handler (for example, to inspect traffic with Fiddler on Windows).

## Consume the Refit endpoint

Inject the interface and call the mapped members; Refit handles serialization for you.

```csharp
public class FactViewModel
{
    private readonly IChuckNorrisEndpoint _endpoint;

    public FactViewModel(IChuckNorrisEndpoint endpoint) => _endpoint = endpoint;

    public async Task<string?> SearchAsync(string term, CancellationToken ct)
    {
        var response = await _endpoint.SearchAsync(ct, term);
        return response.IsSuccessStatusCode
            ? response.Content?.Result.FirstOrDefault()?.Value
            : null;
    }
}
```

The generated proxy reuses the registered `HttpClient`, keeping retries, authentication, and logging consistent across your app.

## Resources

- [Use HttpClientFactory to implement resilient HTTP requests](https://learn.microsoft.com/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#benefits-of-using-ihttpclientfactory)
- [Refit repository](https://github.com/reactiveui/refit)
- [Register HTTP endpoints](xref:Uno.Extensions.Http.HowToHttp)
- [Consume a web API with HttpClient](xref:Uno.Development.ConsumeWebApi)
- [TestHarness Refit samples](https://github.com/unoplatform/uno.extensions/tree/main/testing/TestHarness/TestHarness.Shared/Ext/Http/Refit)
