---
uid: Uno.Extensions.Http.HowToEndpointOptions
---

# How-To: Configure `HttpClient` with Custom Endpoint Options

It's often necessary to include an API key alongside requests to a web API. This can be done by adding a header to the request. The steps below will show you how to easily specify custom options, such as an access token, when adding an endpoint. You can then configure the associated `HttpClient` from these options.

## Pre-requisites

* An [environment](xref:Uno.GetStarted) set up for developing Uno Platform applications

* Basic conceptual understanding of accessing web resources using HTTP requests

* Knowledge of how to [register an endpoint for HTTP requests](xref:Uno.Extensions.Http.HowToHttp)

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Preparing for custom endpoint options

* Create a new class called `CustomEndpointOptions` in the shared project. This class extends `EndpointOptions` to allow you to specify custom options for the endpoint

    ```csharp
    public class CustomEndpointOptions : EndpointOptions
    {
        public string ApiKey { get; set; }
    }
    ```

* In this example, we intend to add an access token to the request header. The `ApiKey` property will be used to store the token.

* The `EndpointOptions` class is a base class that provides a `Url` property. This property is used to specify the URL of the endpoint.

* Subclassing `EndpointOptions` will allow you to configure the `HttpClient` associated with the endpoint — all from a single configuration section.

### 2. Defining the endpoint

* Add `Http` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

    ```diff
    <UnoFeatures>
        Material;
        Extensions;
    +   Http;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

* Enable HTTP by calling the `UseHttp()` method to register a HTTP client with the `IHostBuilder`:

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder.UseHttp();
            });
        ...
    }
    ```

* The `UseHttp()` extension method accepts a callback for configuring the HTTP services as its argument. We will use this callback to register endpoints with the service collection.

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder.UseHttp((ctx, services) => {
                    // Register endpoints here
                });
            });
        ...
    }
    ```

  * `ctx` represents the `HostBuilderContext`. This can be used to access the configuration of the host.

  * `services` is an instance of `IServiceCollection`. This is used to register services with the host.

* An extension method `AddClientWithEndpoint<TInterface, TEndpoint>()` is included which allows specifying **custom endpoint options** when adding a typed client to the service collection.

  * Use this extension method to register a typed client with the service collection and specify custom endpoint options of the type `CustomEndpointOptions`.

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder.UseHttp((ctx, services) => {
                    services.AddClientWithEndpoint<HttpEndpointsOneViewModel, CustomEndpointOptions>();
                });
            });
        ...
    }
    ```

    * Type parameter `TInterface` is the service or view model interface that will be used to access the endpoint.

    * Type parameter `TEndpoint` is the type of the custom endpoint options you define. This type must be a subclass of `EndpointOptions`.

* The extension method above allows you to pass arguments for various details such as the `HostBuilderContext`, an endpoint name (which corresponds to a configuration section), and a callback for configuring the `HttpClient` associated with this endpoint.

  * Add this information to the method call as shown below:

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder.UseHttp((ctx, services) => {
                    services.AddClientWithEndpoint<HttpEndpointsOneViewModel, CustomEndpointOptions>(
                        ctx,
                        name: "HttpDummyJsonEndpoint",
                        configure: (builder, options) =>
                        {
                            builder.ConfigureHttpClient(client =>
                            {
                                    // Configure the HttpClient here
                            });
                        }
                    );
                });
            });
        ...
    }
    ```

    * We assigned the endpoint a name of `HttpDummyJsonEndpoint`. This name corresponds to a configuration section in the `appsettings.json` file. We will add this section in the next section.

    * The `configure` callback is used to configure the `HttpClient` associated with the endpoint.

        > [!TIP]
        > This callback is optional. If you do not need to configure the `HttpClient`, you can omit this callback.

      * Notice that the callback accepts two arguments: `builder` and `options`. `options` is an instance of `CustomEndpointOptions` which we defined earlier. We will use this to access the custom options you defined in the previous section.

      * Add an `ApiKey` to the request headers on the client using the `ConfigureHttpClient` method.

        ```csharp
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var appBuilder = this.CreateBuilder(args)
                .Configure(hostBuilder =>
                {
                    hostBuilder.UseHttp((ctx, services) => {
                        services.AddClientWithEndpoint<HttpEndpointsOneViewModel, CustomEndpointOptions>(
                            ctx,
                            name: "HttpDummyJsonEndpoint",
                            configure: (builder, options) =>
                            {
                                builder.ConfigureHttpClient(client =>
                                {
                                    if (options?.ApiKey is not null)
                                    {
                                        client.DefaultRequestHeaders.Add("ApiKey", options.ApiKey);
                                    }
                                });
                            }
                        );
                    });
                });
            ...
        }
        ```

        * The `ApiKey` header is added to the `HttpClient` using the `DefaultRequestHeaders` property.

        * The value of the header is set to the `ApiKey` property of the `CustomEndpointOptions` instance.

* We have successfully registered an endpoint with the service collection. We will now add a configuration section for this endpoint.

### 3. Adding a configuration section for the endpoint

* Open the `appsettings.json` file and add a configuration section for the endpoint:

    ```json
    {
        "HttpDummyJsonEndpoint": {
            "Url": "https://DummyJson.com",
            "UseNativeHandler": true,
            "ApiKey":  "FakeApiKey"
        }
    }
    ```

  * The name of the configuration section _must_ match the name of the endpoint you specified in the previous section.

  * The `Url` property is used to specify the URL of the endpoint.

  * The `ApiKey` property is used to specify the API key that will be added to the request header.

  * The `UseNativeHandler` property is used to explicitly specify whether to use the native HTTP handler.

### 4. Using the endpoint

* We will now use the endpoint in a view model. Create and a `HttpEndpointsOneViewModel` class with a constructor that accepts an instance of `HttpClient` like so:

    ```csharp
    public class HttpEndpointsOneViewModel
    {
        private readonly HttpClient _client;

        public string? Data { get; internal set;}

        public HttpEndpointsOneViewModel(HttpClient client)
        {
            _client = client;
        }

        public async Task Load()
        {
            Data = await _client.GetStringAsync("products");
        }
    }
    ```

  * The `HttpClient` instance is injected into the view model. This instance is configured with the options we specified in the previous sections.

* All the details of `IHttpClientFactory` are abstracted away from the view model. The view model can simply use this `HttpClient` instance to make requests to the endpoint. The instance can have a managed lifecycle, while a significant amount of ceremony and unintuitive workarounds are avoided.

## See also

* [How-To: Register an Endpoint for HTTP Requests](xref:Uno.Extensions.Http.HowToHttp)
* [How-To: Consume a web API with HttpClient](xref:Uno.Development.ConsumeWebApi)
* [How-To: Create a Strongly-Typed REST Client for an API](xref:Uno.Extensions.Http.HowToRefit)
* [Overview: HTTP](xref:Uno.Extensions.Http.Overview)
* [Overview: Polly and HttpClientFactory](https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory)
* [Explore: TestHarness HTTP Endpoints](https://github.com/unoplatform/uno.extensions/tree/main/testing/TestHarness/TestHarness/Ext/Http/Endpoints/)
