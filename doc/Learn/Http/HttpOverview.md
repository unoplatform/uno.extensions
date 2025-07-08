---
uid: Uno.Extensions.Http.Overview
---
# HTTP

Uno.Extensions.Http allows for the registration of API **endpoints** as multiple typed `HttpClient` instances. In this centralized location for accessing web resources, the lifecycle of the corresponding `HttpMessageHandler` objects is managed. Added clients can optionally be configured to use the platform-native handler. Additional functionality is provided to clear cookies or log diagnostic messages in responses. This library uses [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http) for any HTTP related work.

For additional documentation on HTTP requests, read the references listed at the bottom.

## Installation

`Http`, `HttpRefit` and `HttpKiota` are provided as Uno Features. To enable HTTP client support in your application, add `Http`, `HttpRefit` or `HttpKiota` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

[!include[existing-app](../includes/existing-app.md)]

[!include[single-project](../includes/single-project.md)]

For more information about `UnoFeatures` refer to our [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk) docs.

## Register Endpoints

Web resources exposed through an API are defined in the application as clients. These client registrations include type arguments and endpoints to be used for the client. The endpoint is defined in the `EndpointOptions` class. While it uses the platform-native HTTP handler by default, this value can be configured.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseHttp((context, services) =>
            {
                services
                .AddClient<IShowService, ShowService>(context, "configsectionname");
            });
        });
    ...
}
```

> [!TIP]
> If configuration sections are already used elsewhere, continuing to use that approach offers uniformity and broader accessibility of endpoint options. Consider whether this type of access is needed before using the alternate method below.

`EndpointOptions` can also be loaded from a specific instance.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseHttp((context, services) =>
            {
                services
                .AddClient<IShowService, ShowService>(context,
                    new EndpointOptions
                    {
                        Url = "https://ch9-app.azurewebsites.net/"
                    }
                    .Enable(nameof(EndpointOptions.UseNativeHandler)));
            });
        });
    ...
}
```

### Custom Endpoint Options

`EndpointOptions` is a base class that provides a `Url` property. This property is used to specify the URL of the endpoint. Subclassing `EndpointOptions` allows for custom options beyond the `Url` such as a proxy, timeout, and adding headers. Using this method, the `HttpClient` associated with the endpoint can be configured from a single section in `appsettings.json`.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(hostBuilder =>
        {
            hostBuilder.UseHttp((ctx, services) => {
                services.AddClientWithEndpoint<IShowService, ShowService, CustomEndpointOptions>();
            });
        });
    ...
}
```

For more information about configuring `HttpClient` with custom endpoint options, see the [Configure `HttpClient` with Custom Endpoint Options tutorial](xref:Uno.Extensions.Http.HowToEndpointOptions).

## Refit

Similarly, **Refit endpoints** can be registered as services and configured in a similar way.

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseHttp((context, services) =>
            {
                services
                .AddRefitClient<IChuckNorrisEndpoint>(context);
            });
        });
    ...
}
```

In this case, the endpoint options will be loaded from configuration section _ChuckNorrisEndpoint_ which can be defined as the following JSON:

```json
{
  "ChuckNorrisEndpoint": {
    "Url": "https://api.chucknorris.io/",
    "UseNativeHandler": true
  }
}
```

For more information on using Refit, see the [Quickly Create a Strongly-Typed REST Client for an API tutorial](xref:Uno.Extensions.Http.HowToRefit).

## Kiota

You can generate a strongly-typed client from an **OpenAPI/Swagger** spec using **Kiota**, then register it just like any other endpoint.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseHttp((context, services) =>
            // MyApiClient is generated by Kiota
                services.AddKiotaClient<MyApiClient>(
                    context,
                    options: new EndpointOptions { Url = "https://localhost:5002" }
                )
            );
        });
    ...
}
```

For more information on using Kiota to generate and register your client see the [How-To: Create and register a Kiota client for an API](xref:Uno.Extensions.Http.HowToKiota) guide.

## References

- [How-To: Consume a web API with HttpClient](xref:Uno.Development.ConsumeWebApi)
- [How-To: Register an Endpoint for HTTP Requests](xref:Uno.Extensions.Http.HowToHttp)
- [How-To: Configure with Custom Endpoint Options](xref:Uno.Extensions.Http.HowToEndpointOptions)
- [How-To: Create a Strongly-Typed REST Client for an API](xref:Uno.Extensions.Http.HowToRefit)
- [Overview: Use HttpClientFactory to implement resilient HTTP requests](https://learn.microsoft.com/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#benefits-of-using-ihttpclientfactory)
- [Overview: Delegating handlers](https://learn.microsoft.com/aspnet/web-api/overview/advanced/http-message-handlers)
- [Overview: Polly and HttpClientFactory](https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory)
- [Overview: What is Refit?](https://github.com/reactiveui/refit)
- [Overview: What is Kiota?](https://learn.microsoft.com/en-us/openapi/kiota/)
- [Explore: TestHarness HTTP](https://github.com/unoplatform/uno.extensions/tree/main/testing/TestHarness/TestHarness/Ext/Http/)
