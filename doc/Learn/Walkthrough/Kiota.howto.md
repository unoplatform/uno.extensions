---
uid: Uno.Extensions.Http.Kiota.HowTo
title: Generate Kiota Clients
tags: [http, kiota, openapi, typed-client]
---

> **UnoFeatures:** `HttpKiota` (add to `<UnoFeatures>` in your `.csproj`)

# Generate Kiota clients from OpenAPI

Turn an OpenAPI description into a typed client, register it with Uno Extensions, and wire it to your view models.

## Enable Kiota support

Add the `HttpKiota` UnoFeature to pull in Kiota integration and HTTP plumbing.

```diff
<UnoFeatures>
    Material;
    Extensions;
+   HttpKiota;
    Toolkit;
    MVUX;
</UnoFeatures>
```

This feature adds the `Uno.Extensions.Http.Kiota` package alongside the standard HTTP feature.

## Generate the Kiota client

Use the Kiota CLI to scaffold a client from your OpenAPI document.

```bash
kiota generate \
  --openapi https://localhost:5002/swagger/v1/swagger.json \
  --language CSharp \
  --class-name MyApiClient \
  --namespace-name MyApp.Client.MyApi \
  --output ./MyApp/Content/Client/MyApi
```

Supply either a local file path or a Swagger endpoint URL for `--openapi`, and point `--output` to the folder where you want the generated code.

## Register the Kiota client

Hook the generated client into the Uno host so it receives a configured `HttpClient`.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(hostBuilder =>
        {
            hostBuilder.UseHttp((context, services) =>
            {
                services.AddKiotaClient<MyApiClient>(
                    context,
                    options: new EndpointOptions
                    {
                        Url = "https://localhost:5002"
                    });
            });
        });
}
```

`AddKiotaClient` connects the generated `MyApiClient` to an `HttpClient` whose base address comes from configuration.

## Call the Kiota client

Inject the client where you need it and let the generated methods call the API.

```csharp
public class MyViewModel
{
    private readonly MyApiClient _client;

    public MyViewModel(MyApiClient client) => _client = client;

    public async Task LoadAsync()
    {
        var response = await _client.Api.GetAsync();
        // respond to the API payload
    }
}
```

If you add `Uno.Extensions.Authentication`, the registered handlers automatically attach bearer tokens to each request.

## Resources

- [Kiota overview](https://learn.microsoft.com/en-us/openapi/kiota/)
- [HTTP overview](xref:Uno.Extensions.Http.Overview)
- [Register HTTP endpoints](xref:Uno.Extensions.Http.HowToHttp)
- [Consume a web API with HttpClient](xref:Uno.Development.ConsumeWebApi)
