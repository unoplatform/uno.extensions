# How-To: Use Services with Dependency Injection

Dependency injection (DI) is an important design pattern for building loosely-coupled software that allows for maintainability and testing. This tutorial will walk you through how to register services with a service provider for use throughout your application.

NOTE: This guide assumes you used the Uno.Extensions `dotnet new` template to create the solution. You can verify this by looking for a generated `App.xaml.host.cs` file.

## Step-by-steps

1. Plan the contract for your service
    * Create a new interface which declares the method(s) your service offers:
    ```cs
        public interface IUserProfilePictureService
        {
	        Task<byte[]> GetAsync(CancellationToken ct = null);
        }
    ```

2. Add and register a service that encapsulates app functionality
    * Write a new service class which implements the interface you created above, defining its member(s):
    ```cs
        public class UserProfilePictureService : IUserProfilePictureService
        {
            public async Task<byte[]> GetAsync(CancellationToken ct = null)
            {
                . . .
            }
        }
    ```
    * Register this service implementation with the `IServiceProvider` instance provided by your application's `IHostBuilder`:
    ```cs
        public IHost Host { get; private set; }

        public App()
        {
            Host = UnoHost
                .CreateDefaultBuilder()
                .ConfigureServices(services =>
				{
					// Register your services below
					services.AddSingleton<IUserProfilePictureService, UserProfilePictureService>();
				})
                .Build();
        }
    ```
3. Leverage constructor injection to use an instance of the registered service
    * Create a new view model class that will use the functionality offered by your service. Add a constructor with a parameter of the same type as the service interface you defined earlier:
    ```cs
        public class MainViewModel
        {
            private readonly IUserProfilePictureService userPhotoService;

            public MainViewModel(IUserProfilePictureService userPhotoService)
            {
                this.userPhotoService = userPhotoService;
            }
        }
    ```
    * For the dependency injection framework to handle instantiation of the service as a constructor argument, you must also register your view model with the IServiceProvider:
    ```cs
        public IHost Host { get; private set; }

        public App()
        {
            Host = UnoHost
                .CreateDefaultBuilder()
                .ConfigureServices(services =>
				{
					// Register your services below
					services.AddSingleton<IUserProfilePictureService, UserProfilePictureService>();
                    // Register view model below
                    services.AddTransient<MainViewModel>();
				})
                .Build();
        }        
    ```
    * Now, `MainViewModel` has access to the functionality provided by the resolved implementation of your service:
    ```cs
        byte[] profilePhotoBytes = await userPhotoService.GetAsync();
    ```