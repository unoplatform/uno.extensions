# How to get started with dependency injection

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

2. Add & register a service that encapsulates app functionality
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
        public IHost Host { get; init; }

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
3. Get & use an instance of the registered service
    * You can now retrieve this service in its instantiated form from anywhere in the application such as a view model:
    ```cs
        public class MainViewModel
        {
            private IUserProfilePictureService userPhotoService;

            public MainViewModel()
            {
                userPhotoService = (Application.Current as App).Host.Services.GetService<IUserProfilePictureService>();
            }
        }
    ```
    * Now, `MainViewModel` has access to the functionality provided by your resolved service implementation:
    ```cs
        byte[] profilePhotoBytes = await userPhotoService.GetAsync();
    ```