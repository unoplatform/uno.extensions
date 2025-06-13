---
uid: Uno.Extensions.Http.HowToHttp
---
# How-To: Register an Endpoint for HTTP Requests

When working with a complex application, centralized registration of your API endpoints is a good practice. This allows you to easily change the endpoint for a given service, and to easily add new services.

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Installation

* Add `HttpRefit` (or `HttpKiota` if using Kiota-generated clients) to the `<UnoFeatures>` property in the Class Library (.csproj) file.

    ```diff
    <UnoFeatures>
        Material;
        Extensions;
    -   Http;
    +   HttpRefit;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

    > [!NOTE]
    > As of Uno Platform 6.0, Http no longer includes Uno.Extensions.Http.Refit. Use HttpRefit for Refit-based clients or HttpKiota for Kiota-generated clients.
    > [Migrating to Uno Platform 6.0](xref:Uno.Development.MigratingToUno6)

### 2. Enable HTTP

* Call the appropriate method to register a HTTP client with the `IHostBuilder` which implements `IHttpClient`:
  * Use .UseHttpRefit() for Refit clients
  * Use .UseHttpKiota() for Kiota clients

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder.UseHttp();
                hostBuilder.UseHttpRefit(); // or UseHttpKiota()
            });
        ...
    }
    ```

### 3. Register Endpoints

* The `AddRefitClient` or `AddKiotaClient` extension method is used to register a client with the service collection when using Refit or Kiota respectively in Uno.Extensions.

* While these extension methods can take a delegate as its argument, the recommended way to configure the HTTP client is to specify a configuration section name. This allows you to configure the added HTTP client using the `appsettings.json` file.

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder.UseHttp(services =>
                    services.AddRefitClient<IShowService>("ShowService")
                    // For Kiota:
                    // hostBuilder.UseHttpKiota(services =>
                    //     services.AddKiotaClient<IShowService>("ShowService")
                    // );
                );
            });
        ...
    }
    ```

* Ultimately, your service will be based on the functionality provided by the web API, but the `IShowService` interface will be implemented by Refit or Kiota and injected into your application at runtime. You will make requests to the registered endpoint through this interface. In this case, the service interface will look something like this:

    ```csharp
    public interface IShowService
    {
        Task<Show> GetShowAsync();
    }
    ```

* The endpoint is defined in the `appsettings.json` file. While the default behavior is to use the platform-native HTTP handler, this can be configured.

    ```json
    {
        "ShowService": {
            "Url": "https://ch9-app.azurewebsites.net/",
            "UseNativeHandler": true
        }
    }
    ```

### 4. Use the Service to Request Data

* Since you registered the service with the service collection, you can now inject the `IShowService` implementation into your view models and use it to request information about a show from the endpoint:

    ```csharp
    public class ShowViewModel : ObservableObject
    {
        private readonly IShowService _showService;

        public ShowViewModel(IShowService showService)
        {
            _showService = showService;
        }

        public async Task LoadShowAsync()
        {
            var show = await _showService.GetShowAsync();
            ...
        }
        ...
    }
    ```

## See also

* [How-To: Consume a web API with HttpClient](xref:Uno.Development.ConsumeWebApi)
* [How-To: Create a Strongly-Typed REST Client for an API](xref:Uno.Extensions.Http.HowToRefit)
* [How-To: Configure with Custom Endpoint Options](xref:Uno.Extensions.Http.HowToEndpointOptions)
* [Overview: HTTP](xref:Uno.Extensions.Http.Overview)
* [Overview: Polly and HttpClientFactory](https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory)
* [Explore: TestHarness HTTP Endpoints](https://github.com/unoplatform/uno.extensions/tree/main/testing/TestHarness/TestHarness/Ext/Http/Endpoints/)
