# Dependency injection

Uno.Extensions.Hosting use [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting) for any IoC related work.

For more documentation on dependency injection, read the references listed at the bottom.

## Registering

- You can register a singleton service using `services.AddSingleton<IService, ServiceImplementation>()`. This service will be shared by every user.

  - You don't need to specify any constructor parameters if all dependencies can be resolved automatically. Otherwise, you can use `services.AddSingleton<IService>(s => new ServiceImplementation(..))`.

- You can register a service that will be created everytime you request it by using `services.AddTransient<IService, ServiceImplementation>()`. 

- You can use `TryAdd*` which will register the service only if there hasn't been a registration of that type yet.

- You can't register named dependencies as this is [generally a bad practice](https://stackoverflow.com/questions/46476112/dependency-injection-of-multiple-instances-of-same-type-in-asp-net-core-2).

## Resolving

- Dependencies are injected automatically into the constructors of the registered services.

  ```csharp
  services.AddSingleton<MyService>();
  services.AddSingleton<MyOtherService>();

  public class MyService(MyOtherService myOtherService)
  {
    // Resolving MyService will automatically add MyOtherService here.
  }
  ```

- If you can't use constructor injection, you can use `IServiceProvider.GetRequiredService<IService>()` to resolve a service.
This will throw an exception if the type `IService` is not registered.
You can also use `IServiceProvider.GetService<IService>()` which will return `default(IService)` if the type is not registered.

- Circular dependencies will not work with this container. If you do have them, you will get the following exception `A circular dependency was detected for the service of type`.

- You can access the service provider **statically** using `App.Instance.Startup.ServiceProvider`.

- You can access your services from a **view model** using `this.GetService<MyService>`.

## CommunityToolkit.Mvvm

If you want to access the DI container via the Ioc.Default API exposed via the CommunityToolkit, you need to configure the service collection after building the Host.

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .Build();
    Ioc.Default.ConfigureServices(Host.Services);
    // ........ //
}
```



## References

- [Understanding Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.0)
- [Using dependency injection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.0)
