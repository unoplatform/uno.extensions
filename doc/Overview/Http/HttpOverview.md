---
uid: Overview.Http
---
# HTTP

Uno.Extensions.Http allows for the registration of API endpoints as multiple typed `HttpClient` instances. In this centralized location for accessing web resources, the lifecycle of the corresponding `HttpMessageHandler` objects is managed. Added clients can optionally be configured to use the platform-native handler. Additional functionality is provided to clear cookies or log diagnostic messages in responses. This library uses [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http) for any HTTP related work.

For more documentation on HTTP requests, read the references listed at the bottom.

## Register Endpoints

Web resources exposed through an API are defined in the application as clients. These client registrations include type arguments and endpoints to be used for the client. The endpoint is defined in the `EndpointOptions` class. While it uses the platform-native HTTP handler by default, this value can be configured. 

```csharp
private IHost Host { get; }

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
```

`EndpointOptions` can also be loaded from a specified configuration section name. Refer to the [Configuration](xref:Overview.Configuration) documentation for more information.

```csharp
private IHost Host { get; }

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
```

## Refit

Refit endpoints can be configured as services in a similar way. 

```csharp
private IHost Host { get; }

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
```

In this case, the `EndpointOptions` will be loaded from configuration section ChuckNorrisEndpoint. The configuration section could be defined as follows:

```json
{
  "ChuckNorrisEndpoint": {
    "Url": "https://api.chucknorris.io/",
    "UseNativeHandler": true
  }
}
```

See the [tutorial](xref:Learn.Tutorials.Http.HowToRefit) for more information on using Refit.

## References
- [Making HTTP requests using IHttpClientFactory](https://learn.microsoft.com/aspnet/core/fundamentals/http-requests)
- [Delegating handlers](https://learn.microsoft.com/aspnet/web-api/overview/advanced/http-message-handlers)
- [Polly and HttpClientFactory](https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory)
- [What is Refit?](https://github.com/reactiveui/refit)