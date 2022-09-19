---
uid: Overview.Hosting
---
# Hosting

Hosting is provided by [Microsoft.Extensions.Hosting.UWP](https://www.nuget.org/packages/Uno.Extensions.Hosting.UWP) or [Microsoft.Extensions.Hosting.WinUI](https://www.nuget.org/packages/Uno.Extensions.Hosting.WinUI).

## Getting started

The `IHost` instance for the application should be created as soon as the application instance is created. The following snippet uses the UnoHost static class to create the IHost implementation using the generic host builder. 

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .Build();
    // ........ //
}
```

## Service Initialization

Some service instances need to be created and initialized as soon as possible after the `IHost` has been built. The `IServiceInitialize` interface identifies services that need to be created and initialize immediately after the `IHost` instance is created 

```csharp 
public interface IServiceInitialize
{
	void Initialize();
}
```

> [!TIP]
> Avoid using the `IServiceInitialize` interface unless absolutely required as it will add to the startup time for the application. It's recommended to implement the IHostedService interface for services that can be created and started asynchronously (see next section).


## Async Initialization with IHostedService

The initialization of the application hosting is intentionally a synchronous process which makes it unsuitable for long running initialization code. Asynchronous initialization can be done by registering an implementation of IHostedService. eg:

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

The implementation can be registered during the host initialization eg:

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .ConfigureServices(service => service.AddHostedService<SimpleHostService>())
        .Build();
    // ........ //
}
```

In order for hosted services to be run, it is necessary to run the IHost implementation. It's recommended to do this in the OnLaunched method of the App.xaml.cs. This will ensure the application instance, and associated window, is accessible, should you want to initialize anything related to the UI. eg:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    // ........ //
    await Task.Run(()=>Host.StartAsync());
}
```

If you require your hosted service to complete startup prior to the first navigation (if using [Navigation](xref:uid: Overview.Navigation)) you can implement the `IStartupService` interface, returning a task that can be awaited in the `StartupComplete` method. This might be useful for pre-loading data in order to work out which view to navigate to eg:

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

## Hosting environments

As part of initializing the host, an instance of IHostEnvironment is registered and can be retrieved in order to determine information about the current hosting environment. eg:

```csharp
var env = Host.Services.GetService<IHostEnvironment>();
Debug.WriteLine($"Environment: {env.EnvironmentName}");
```

The current hosting environment can be changed using the UseEnvironment static method on the IHostBuilder. eg:

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseEnvironment("Staging")
        .Build();
    // ........ //
}
```

The current hosting environment can also used when configuring the host builder. eg:

```csharp
    UnoHost
        // ........ //
        .ConfigureServices((context, services) => {
            var isDevelopment = context.HostingEnvironment.IsDevelopment();
            var isStaging = context.HostingEnvironment.IsStaging();
            var isProduction = context.HostingEnvironment.IsProduction();
            var environment = context.HostingEnvironment.EnvironmentName;
            var isMyEnvironment = context.HostingEnvironment.IsEnvironment("MyEnvironment");
            })
        // ........ //
        .Build();
```

> [!TIP]
> In general it's good to avoid writing code that contains logic specific to any environment. It's preferable to have all environments behave as close as possible to each other, thus minimizing any environment specific bugs that may be introduced by environment specific code.  
> Any environment specific secure variables should be set as part of a multi-environment CI/CD pipeline. For example service urls, application key, account information etc. Non-secure per-environment variables can be included using a settings file (this is covered in [Configuration](xref:uid: Overview.Configuration).  

