---
uid: Learn.Tutorials.Http.HowToHttp
---
# How-To: Register an Endpoint for HTTP Requests

When working with a complex application, centralized registration of your API endpoints is a good practice. This allows you to easily change the endpoint for a given service, and to easily add new services.

## Step-by-steps

> [!IMPORTANT]
> This guide assumes you used the template wizard or `dotnet new unoapp` to create your solution. If not, it is recommended that you follow the [instructions](xref:Overview.Extensions) for creating an application from the template.

### 1. Enable HTTP

* Call the `UseHttp()` method to register a HTTP client with the `IHostBuilder` which implements `IHttpClient`:

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder.UseHttp();
            });
    ...
    ```

### 2. Register Endpoints

* The `AddClient` extension method is used to register a client with the service collection. 

* While the `AddClient()` extension method can take a delegate as its argument, the recommended way to configure the HTTP client is to specify a configuration section name. This allows you to configure the added HTTP client using the `appsettings.json` file. 

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder.UseHttp(services =>
                    services.AddClient<IShowService, ShowService>("ShowService")
                );
            });
    ...
    ```

* Ultimately, your service will be based on the functionality provided by the web API, but the `HttpClient` associated with it will be injected into the constructor of your service implementation. You will make requests to the registered endpoint inside your service implementation. In this case, the service interface will look something like this:
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

### 3. Use the Service to Request Data

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
    ```

## See also

- [How-To: Consume a web API with HttpClient](xref:Uno.Development.ConsumeWebApi)
- [Polly and HttpClientFactory](https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory)
- [How-To: Quickly Create a Strongly-Typed REST Client for an API](xref:Learn.Tutorials.Http.HowToRefit)