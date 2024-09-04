---
uid: Uno.Extensions.DependencyInjection.HowToCommunityToolkit
---

# How-To: Manually Resolving Dependencies with CommunityToolkit.Mvvm

While making gradual changes to an existing app's codebase, you may find it necessary to access the DI container to manually resolve dependencies. For instance, if you are overhauling a view model to separate its logic into services, you may need to resolve the service without using constructor injection. The [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm) package provides a static `Ioc.Default` property that exposes the DI container used by the application.

This tutorial will walk you through how to set up this feature and use it to manually resolve dependencies.

> [!WARNING]
> This approach to resolving dependencies is _not_ recommended, and should serve primarily as a stopgap measure while you refactor your codebase to use constructor injection.

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Add CommunityToolkit.Mvvm to your project

* Add `Mvvm` to the `<UnoFeatures>` property in the Class Library (.csproj) file. This will add the [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm) package to your project.

    ```diff
    <UnoFeatures>
        Material;
        Extensions;
    +   Mvvm;
        Toolkit;
    </UnoFeatures>
    ```

### 2. Register services with the DI container

* Register services with the DI container as you normally would. For instance, you can use the `Configure` method on `IHostBuilder` to register services with the DI container:

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder => {
                hostBuilder.ConfigureServices(services =>
                    services
                        .AddSingleton<IPrimaryService, PrimaryService>()
                        .AddSingleton<ISecondaryService, SecondaryService>()
                );
            });

        Host = appBuilder.Build();
    }
    ```

### 3. Configure the DI container to use the CommunityToolkit.Mvvm service provider

* Observe that the built `IHost` instance is available to the `App.cs` file. It is stored in a property on the `App` class:

    ```csharp
    private IHost Host { get; set; }
    ```

* Because the `IHost` instance is available to the `App.cs` file, you can get the `IHost.Services` collection and configure the service provider to use it.

* To do so, add the following line to the `OnLaunched` method:

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ...
        Ioc.Default.ConfigureServices(Host.Services);
    }
    ```

### 4. Resolve services from the DI container

* You can now resolve services from the DI container using the `Ioc.Default` property. For instance, you can resolve the `IPrimaryService` service in a view model that is not registered with the DI container.

* Do this by calling `GetService` or `GetRequiredService` method on the singleton provider instance:

    ```csharp
    public class MainViewModel : ObservableRecipient
    {
        private readonly IPrimaryService? primaryService;

        private readonly ISecondaryService secondaryService;

        public MainViewModel()
        {
            // Get the IPrimaryService instance if available; otherwise returns null.
            primaryService = Ioc.Default.GetService<IPrimaryService>();

            // Get the ISecondaryService instance if available; otherwise throws an exception.
            secondaryService = Ioc.Default.GetRequiredService<ISecondaryService>();
        }
    }
    ```

## See also

* [Dependency injection in Uno Extensions tutorial](xref:Uno.Extensions.DependencyInjection.HowToDependencyInjection)
* [CommunityToolkit.Mvvm NuGet package](https://www.nuget.org/packages/CommunityToolkit.Mvvm)
* [Ioc class](https://learn.microsoft.com/dotnet/api/communitytoolkit.mvvm.dependencyinjection.ioc)
* [Inversion of control](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/ioc)
