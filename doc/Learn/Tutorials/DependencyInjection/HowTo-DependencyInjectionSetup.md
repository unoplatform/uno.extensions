---
uid: Learn.Tutorials.DependencyInjection.HowToDependencyInjection
---
# How-To: Use Services with Dependency Injection

Dependency Injection (DI) is an important design pattern for building loosely-coupled software that allows for maintainability and testing. This tutorial will walk you through how to register services so that they can be consumed throughout your application.

> [!TIP]
> This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions` template to create the solution. Instructions for creating an application from the template can be found [here](xref:Overview.Extensions)

## Step-by-steps

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
            . . .
        }
    }
    ```
### 3. Register your service
* Register this service implementation with the `IServiceCollection` instance provided by your application's `IHostBuilder`, in the `app.xaml.host.cs` file:
    ```cs
    private IHost Host { get; } = BuildAppHost();

    private static IHost BuildAppHost()
	{ 
		return UnoHost
            .CreateDefaultBuilder()
            .ConfigureServices(services =>
			{
			    // Register your services
				services.AddSingleton<IProfilePictureService, ProfilePictureService>();
			})
            .Build();
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
* For the dependency injection framework to handle instantiation of the service as a constructor argument, you must also register your view model with the `IServiceCollection` (or register the view model via routing):
    ```cs
    private IHost Host { get; } = BuildAppHost();

    private static IHost BuildAppHost()
    { 
        return UnoHost
            .CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Register your services
		services.AddSingleton<IProfilePictureService, ProfilePictureService>();
                // Register view model
                services.AddTransient<MainViewModel>();
            })
            .Build();
    }        
    ```
* Now, `MainViewModel` has access to the functionality provided by the implementation of your service resolved by `IServiceProvider`:
    ```cs
    byte[] profilePhotoBytes = await userPhotoService.GetAsync(cancellationToken);
    ```
### 5. Create ViewModel 
* From the code behind of a view, directly reference the application's `IHost` instance to request an instance of the desired view model. Set this as the `DataContext`:
    ```cs
        public MainPage()
        {
            this.InitializeComponent();
            DataContext = (Application.Current as App).Host.Services.GetRequiredService<MainViewModel>();
        }
    ```
> [!TIP]
> By default the `Host` property is marked as `private`, so you'll need to change it to `public` in order for the above code to work. Alternatively if you use [Navigation](xref:Overview.Navigation), view model classes are automatically connected with the corresponding page, avoiding having to access the `IServiceProvider` directly. 
