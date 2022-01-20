# Hosting

The Guidance Template uses Uno.Extensions.Hosting to configure the hosting environment for the application. This includes loading configuration information, setting up a dependency container and configure logging for the application. This builds on the generic host provided by [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting).

## Getting started

The hosting for the application should be created as soon as the application instance is created. In the following snippet the UnoHost static class is used to create the IHost implementation using the generic host builder. 

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

## Async Initialization

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
    await Task.Run(async () =>
    {
    	await Host.StartAsync();
    });
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

**NOTE: In general it's good to avoid writing code that contains logic specific to any environment. It's preferable to have all environments behave as close as possible to each other, thus minimizing any environment specific bugs that may be introduced by environment specific code.

Any environment specific secure variables should be set as part of a multi-environment CI/CD pipeline. For example service urls, application key, account information etc. Non-secure per-environment variables can be included using a settings file (this is covered in [Configuration](./02 Configuration.md).  

## WASM
Do not add a reference to Uno.Extensions.Hosting in the WASM project. Instead, add a reference to Uno.Extensions.Hosting.Wasm package.


