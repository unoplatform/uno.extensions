---
uid: Uno.Extensions.DependencyInjection.HowToDependencyInjection
---
# How-To: Use Services with Dependency Injection

Dependency Injection (DI) is an important design pattern when building loosely-coupled software that allows for maintainability and testing. This tutorial will walk you through how to register services so that they can be consumed throughout your application.

## Step-by-step

[!include[create-application](../includes/create-application.md)]

> [!IMPORTANT]
> If you created your app without setting up Hosting, make sure to check out the [Hosting setup](xref:Uno.Extensions.Hosting.HowToHostingSetup) first so you have everything in place before continuing with this guide.

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
    }
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

* If you are using not using [navigation](xref:Uno.Extensions.Navigation.Overview), you have to register the view model to `IServiceCollection`, but we recommend using navigation and not manually register the view model as a service:

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
                        // Register view model
                        services.AddTransient<MainViewModel>();
                    }
                );
            });
        ...
    }
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
> By default the `Host` property is marked as `private`, so you'll need to change it to `public` in order for the above code to work. Alternatively, if you use [Navigation](xref:Uno.Extensions.Navigation.Overview), view model classes are automatically connected with the corresponding page, avoiding having to access the `IServiceProvider` directly.
