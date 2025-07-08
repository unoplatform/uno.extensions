---
uid: Uno.Extensions.Http.HowToKiota
---
# How-To: Quickly create and register a Kiota Client for an API

When working with APIs in your application, having a strongly-typed client can simplify communication and reduce boilerplate code. **Kiota** is a tool that generates strongly-typed API clients from Swagger/OpenAPI definitions. With Uno.Extensions, you can easily register and use Kiota clients in your Uno Platform app without additional setup.

## Step-by-Step Guide

> [!IMPORTANT]
> This guide assumes you used the template wizard or `dotnet new unoapp` to create your solution. If not, it is recommended that you follow the [**Creating an application with Uno.Extensions** documentation](xref:Uno.Extensions.HowToGettingStarted) to create an application from the template.

### 1. Installation

* Add `HttpKiota` to the `<UnoFeatures>` property in the Class Library (`.csproj`) file:

    ```diff
    <UnoFeatures>
        Material;
        Extensions;
    +   HttpKiota;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

### 2. Enable Http in Host Builder

* Add the UseHttp method to the `IHostBuilder`:

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

### 3. Generate the Kiota Client

* Install the Kiota tool:

    ```bash
    dotnet tool install --global Microsoft.OpenApi.Kiota
    ```

* Generate the Client using the OpenAPI specification URL or a local file:

    ```bash
    # From a static spec file
    kiota generate --openapi PATH_TO_YOUR_API_SPEC.json --language CSharp --class-name MyApiClient --namespace-name MyApp.Client.MyApi --output ./MyApp/Content/Client/MyApi
    # OR directly from the running server’s Swagger endpoint
    kiota generate --openapi http://localhost:5002/swagger/v1/swagger.json --language CSharp --class-name MyApiClient --namespace-name MyApp.Client.MyApi --output ./MyApp/Content/Client/MyApi
    ```

    This will create a client named `MyApiClient` in the Client folder.

### 4. Register the Kiota Client

* Register the generated client in the IHostBuilder using `AddKiotaClient` from `Uno.Extensions`:

    ```csharp

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder.UseHttp((context, services) =>
                    services.AddKiotaClient<MyApiClient>(
                        context,
                        options: new EndpointOptions { Url = "https://localhost:5002" }
                    )
                );
            });
    }
    ```

### 5. Use the Kiota Client in Your Code

* Inject the ApiClient into your view model or service and make API requests:

```csharp
public class MyViewModel
{
    private readonly MyApiClient _apiClient;

    public MyViewModel(MyApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task GetAll()
    {
        var something = await _apiClient.Api.GetAsync();
        Console.WriteLine($"Retrieved {something?.Count}.");
    }
}

```

## Important Considerations

* With `Uno.Extensions.Authentication`, the HttpClient automatically includes the **Authorization** header. You don't need to manually handle token injection. The middleware ensures the access token is included in each request.

* Ensure your server is running and the swagger.json file is accessible at the specified URL when generating the Kiota client.

## See also

* [Overview: What is Kiota?](https://learn.microsoft.com/en-us/openapi/kiota/)
* [Overview: HTTP](xref:Uno.Extensions.Http.Overview)
* [How-To: Consume a web API with HttpClient](xref:Uno.Development.ConsumeWebApi)
* [How-To: Register an Endpoint for HTTP Requests](xref:Uno.Extensions.Http.HowToHttp)
