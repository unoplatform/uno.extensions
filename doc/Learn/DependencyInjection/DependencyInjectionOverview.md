---
uid: Uno.Extensions.DependencyInjection.Overview
---

# Dependency Injection

Apps built using hosting can leverage dependency injection (DI) to register services and make them available to app dependencies. This pattern enables apps to follow sound design principles, such as [SOLID](https://en.wikipedia.org/wiki/SOLID) and [Inversion of Control](https://en.wikipedia.org/wiki/Inversion_of_control). While the host builder implements standard functionality from the [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) library, it also provides additional features to improve the developer experience.

## Registering Services

Services are registered with the host using the `ConfigureServices` method on the `IHostBuilder`. The following snippet shows how to register a service with the host:

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<SimpleService>();
            })
        });

    Host = appBuilder.Build();
    ...
}
```

Before the `IHost` instance is created, the `ConfigureServices` method is called to register services with the host. The `ConfigureServices` method is called from the lambda expression passed into the `Configure` method. `ConfigureServices` itself takes two parameters: a `HostBuilderContext` and an `IServiceCollection`. The `HostBuilderContext` provides access to the host's configuration and environment. The `IServiceCollection` is used to register services with the host.

## Resolving Services

The recommended way to resolve services is to use constructor injection. The following snippet shows how to resolve a service from the host:

```csharp
public class SimpleViewModel : ObservableObject
{
    public SimpleViewModel(ISimpleService service)
    {
        Service = service;
    }

    public ISimpleService Service { get; }
}
```

Services can also be resolved from an `IHost` instance:

```csharp
var service = Host.Services.GetService<ISimpleService>();
```

## Service Lifetimes

Services can be registered with the host using different lifetimes. The following table shows the different service lifetimes:

| Lifetime | Description |
|----------|-------------|
| `Transient` | A new instance of the service is created each time it is requested. |
| `Scoped` | A single instance of the service is created per scope. |
| `Singleton` | A single instance of the service is created for the lifetime of the host. |

Typically, services are registered with the `Singleton` lifetime.

## Named Services

Uno.Extensions provides a way to register multiple services of the same type with different names. This is useful in scenarios where multiple implementations of the same service are configured differently in an implementation factory and the correct implementation needs to be resolved at runtime. The following snippet shows how to register a named service with the host:

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host
            .ConfigureServices((context, services) =>
            {
                services.AddNamedSingleton<ISimpleService, SimpleService>("SimpleNamedServiceOne");
                services.AddNamedSingleton<ISimpleService, SimpleService>("SimpleNamedServiceTwo");
            })
        });

    Host = appBuilder.Build();
    ...
}
```

Services can be resolved by name using the `GetNamedService` extension method:

```csharp
var service = Host.Services.GetNamedService<ISimpleService>("SimpleNamedServiceOne");
```

## Service Implementation Factories

Uno.Extensions provides a way to register services using a factory method. This is useful in scenarios where the service implementation is not known until runtime. The following snippet shows how to register a service with the host using a factory method:

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<ISimpleService>(serviceProvider =>
                {
                    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                    var simpleService = new SimpleService();
                    simpleService.Configure(configuration);
                    return simpleService;
                });
            })
        });

    Host = appBuilder.Build();
    ...
}
```
