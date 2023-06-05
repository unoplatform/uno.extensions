---
uid: Learn.Tutorials.DependencyInjection.HowToDependencyInjection
---
# How-To: Use Services with Dependency Injection

Dependency Injection (DI) is an important design pattern when building loosely-coupled software that allows for maintainability and testing. This tutorial will walk you through how to register services so that they can be consumed throughout your application.

## Step-by-steps

> [!IMPORTANT]
> This guide assumes you used the template wizard or `dotnet new unoapp` to create your solution. If not, it is recommended that you follow the [instructions](xref:Overview.Extensions) for creating an application from the template.

### 1. Plan the contract for your service  
* Create a new interface which declares the method(s) your service offers: 
    ```cs
    public interface IProfilePictureService
    {
        Task<byte[]> GetAsync(CancellationToken ct);
    }
    ```

### 2. Create the service implementation that encapsulates app functionality 
* Write a new service class which implements the interface you created above, defining its member(s):
    ```cs
    public class ProfilePictureService : IProfilePictureService
    {
        public async Task<byte[]> GetAsync(CancellationToken ct)
        {
            ...
        }
    }
    ```
### 3. Register your service
* Register this service implementation with the `IServiceCollection` instance provided by your application's `IHostBuilder`:

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder
                    .ConfigureServices(services =>
                    {
                        // Register your services
                        services.AddSingleton<IProfilePictureService, ProfilePictureService>();
                    }
                );
            });
    ...
    ```
### 4. Use the service
* Create a new view model class, `MainViewModel`, that will use the functionality offered by your service. Add a constructor with a parameter of the same type as the service interface you defined earlier:
    ```cs
    public class MainViewModel
    {
        private readonly IProfilePictureService userPhotoService;

        public MainViewModel(IProfilePictureService userPhotoService)
        {
            this.userPhotoService = userPhotoService;
        }
    }
    ```
* For the dependency injection framework to handle instantiation of the service as a constructor argument, you must also register your view model with the `IServiceCollection`:
    ```cs
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder
                    .ConfigureServices(services =>
                    {
                        // Register your services
                        services.AddSingleton<IProfilePictureService, ProfilePictureService>();
                        services.AddTransient<MainViewModel>();
                    }
                );
            });
    ...     
    ```
* Now, `MainViewModel` has access to the functionality provided by the implementation of your service resolved by `IServiceProvider`:
    ```cs
    byte[] profilePhotoBytes = await userPhotoService.GetAsync(cancellationToken);
    ```

### 5. Set DataContext to view model
* From the code behind of a view, get an instance of the desired view model. Set this as the `DataContext`:
    ```cs
        public MainPage()
        {
            this.InitializeComponent();
            DataContext = (Application.Current as App).Host.Services.GetRequiredService<MainViewModel>();
        }
    ```
> [!TIP]
> By default the `Host` property is marked as `private`, so you'll need to change it to `public` in order for the above code to work. Alternatively, if you use [Navigation](xref:Overview.Navigation), view model classes are automatically connected with the corresponding page, avoiding having to access the `IServiceProvider` directly. 