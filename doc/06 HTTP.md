# HTTP
Uno.Extensions.Http uses [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http) for any HTTP related work.

For more documentation on HTTP requests, read the references listed at the bottom.

## Register Endpoints

Register native http handlers
Register http client for specific service with `EndpointOptions`.

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
			   {
				   _ = services
    				   .AddNativeHandler()
    				   .AddClient<IShowService, ShowService>(context,
						   new EndpointOptions
							   {
								   Url = "https://ch9-app.azurewebsites.net/"
							   }
							   .Enable(nameof(EndpointOptions.UseNativeHandler))
					   );
			   })
        .Build();
    // ........ //
}
```

`EndpointOptions` can also be loaded from configuration section. Specify the name of the configuration section to load.

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
			   {
				   _ = services
    				   .AddNativeHandler()
    				   .AddClient<IShowService, ShowService>(context, "configsectionname");
			   })
        .Build();
    // ........ //
}
```

## Refit

Refit endpoints can be configured as services. 

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
			   {
				   _ = services
    				    .AddNativeHandler()
    			        .AddRefitClient<IChuckNorrisEndpoint>(context);
                })
        .Build();
    // ........ //
}
```

In this case the EndpointOptions will be loaded from configuration section ChuckNorrisEndpoint.



## References
- [Making HTTP requests using IHttpClientFactory](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.0)
- [Delegating handlers](https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers)
- [Polly and HttpClientFactory](https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory)
- [What is Refit](https://github.com/reactiveui/refit)
