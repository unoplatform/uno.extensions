---
uid: Uno.Extensions.Hosting.Overview
---
# Hosting

`Hosting` provides an implementation of the abstraction for building applications which support initialization of dependencies, establishing different environment types, and the full breadth of extensible features offered by Uno.Extensions.

Hosting is provided as an Uno Feature. To enable `Hosting` support in your application, add `Hosting` to the `<UnoFeatures>` property in the Class Library (.csproj) file. For more information about `UnoFeatures` refer to our [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk) docs.

[!include[existing-app](../includes/existing-app.md)]

[!include[single-project](../includes/single-project.md)]

[!include[getting-help](../includes/getting-help.md)]

## Building a Hosted Application

Initialization of the `IHost` instance is done from the generated App.cs file of your solution. It should be created as soon as the application is launched. The following snippet uses the `CreateBuilder()` extension method to instantiate an `IApplicationBuilder` from your `Application`. It is then possible to configure the associated `IHostBuilder` to register services or use the numerous extensions offered by this library.

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            // Configure the host builder
        });

    Host = appBuilder.Build();
    ...
}
```

For a more specific tutorial about getting started with building hosted applications, see [Get Started with Hosting](xref:Uno.Extensions.Hosting.HowToHostingSetup).

## Service Initialization

Services are the primary way to access application functionality. Services are registered with the host using the `ConfigureServices` method on the `IHostBuilder`. Some services need to be created and initialized as soon as possible after the `IHost` has been built. The `IServiceInitialize` interface identifies services that need to be created and initialize immediately after the `IHost` instance is created.

```csharp
public interface IServiceInitialize
{
    void Initialize();
}
```

> [!TIP]
> Avoid using the `IServiceInitialize` interface unless absolutely required as it will add to the startup time for the application. It's recommended to implement the `IHostedService` interface for services that can be created and started asynchronously (see next section).

## Async Initialization with IHostedService

The initialization of the application hosting is intentionally a synchronous process which makes it unsuitable for long running initialization code. Asynchronous initialization can be done by registering an implementation of `IHostedService`.

```csharp
public class SimpleHostService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```

The implementation can be registered during the host initialization by calling the `AddHostedService` method on the `IServiceCollection` returned by the `ConfigureServices` method on the `IHostBuilder`.

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<SimpleHostService>()
            })
        });

    Host = appBuilder.Build();
}
...
```

In order for hosted services to be run, it is necessary to run the `IHost` implementation. It's recommended to do this in the OnLaunched method of the App.cs. This will ensure the application instance and associated window are accessible, should initialization of anything related to the UI be required.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    ...
    await Task.Run(() => Host.StartAsync());
}
```

If a hosted service is required to complete startup prior to the first navigation (if using [Navigation](xref:Uno.Extensions.Navigation.Overview)), implement the `IStartupService` interface. A task will be returned that can be awaited in the `StartupComplete` method. This technique might be useful for pre-loading data in order to work out which view to navigate to.

```csharp
public class SimpleStartupService : IHostedService, IStartupService
{
    private TaskCompletionSource<object> _completion = new TaskCompletionSource<object>();
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _completion.SetResult(true);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartupComplete()
    {
        return _completion.Task;
    }
}
```

## Hosting Environments

As part of initializing the host, an instance of `IHostEnvironment` is registered and can be retrieved to determine information about the current hosting environment.

```csharp
var env = Host.Services.GetService<IHostEnvironment>();
Debug.WriteLine($"Environment: {env.EnvironmentName}");
```

The current hosting environment can be changed with the `UseEnvironment()` extension method.

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseEnvironment("Staging")
        });

    Host = appBuilder.Build();
...
```

The current hosting environment can also be used when configuring the host builder.

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .ConfigureServices((context, services) =>
            {
                var isDevelopment = context.HostingEnvironment.IsDevelopment();
                var isStaging = context.HostingEnvironment.IsStaging();
                var isProduction = context.HostingEnvironment.IsProduction();
                var environment = context.HostingEnvironment.EnvironmentName;
                var isMyEnvironment = context.HostingEnvironment.IsEnvironment("MyEnvironment");
            })
        });

    Host = appBuilder.Build();
...
```

> [!TIP]
> Avoid writing code that contains logic specific to any environment. All environments should behave as close as possible to each other to minimize any environment specific bugs that may be introduced by environment specific code.
>
> Any environment specific secure variables (such as service URLs, application keys, account information) should be set as part of a multi-environment CI/CD pipeline. Non-secure per-environment variables can be included using a settings file which is covered in [Configuration](xref:Uno.Extensions.Configuration.Overview).
