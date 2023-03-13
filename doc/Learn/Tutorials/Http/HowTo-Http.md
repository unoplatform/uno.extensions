---
uid: Learn.Tutorials.Http.HowToHttp
---
# How-To: Register an Endpoint for HTTP Requests

When working with a complex application, centralized registration of your API endpoints is a good practice. This allows you to easily change the endpoint for a given service, and to easily add new services.

## Step-by-steps

### 1. Enable HTTP

* Call the `UseHttp()` method to register a HTTP client that implements `IHttpClient` with the service collection:

    ```csharp
    private IHost Host { get; }
    
    public App()
    {
        Host = UnoHost
            .CreateDefaultBuilder()
            .UseHttp()
            .Build();
    ...
    ```

### 2. Register Endpoints

* The `UseHttp()` method accepts a delegate that is used to configure the HTTP client. The delegate is passed the `IHostBuilderContext` and the `IServiceCollection`:

    ```csharp
    private IHost Host { get; }
    
    public App()
    {
        Host = UnoHost
            .CreateDefaultBuilder()
            .UseHttp((context, services) =>
                   {
                       // ...
                   })
            .Build();
    ...
    ```

* Ultimately, your service will be based on the functionality provided by the web API, but the `HttpClient` associated with it will be injected into the constructor of your service implementation. You will make requests to the registered endpoint inside your service implementation. In this case, the service interface will look something like this:
    ```csharp
    public interface IShowService
    {
        Task<Show> GetShowAsync(SourceFeed sourceFeed = null);
    }
    ```

* The `AddClient` method is used to register a client with the service collection. When you use the `AddClient` method, pass in the `IHostBuilderContext`, the type of the service, the type of the client, and the endpoint to use for the client. 

* The endpoint is defined in the `EndpointOptions` class. The `EndpointOptions` class can be configured to use the platform-native HTTP handler. 

    ```csharp
    private IHost Host { get; }
    
    public App()
    {
        Host = UnoHost
            .CreateDefaultBuilder()
            .UseHttp((context, services) =>
                   {
                       _ = services
                           .AddClient<IShowService, ShowService>(context,
                               new EndpointOptions
                                   {
                                       Url = "https://ch9-app.azurewebsites.net/"
                                   }
                                   .Enable(nameof(EndpointOptions.UseNativeHandler))
                           );
                   })
            .Build();
    ...
    ```

### 3. Use the Service to Request Data

* Since you registered the service with the service collection, you can now inject the `IShowService` implementation into your view models and use it to request data from the endpoint:

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