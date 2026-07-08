---
uid: Uno.Extensions.DependencyInjection.CommunityToolkit.HowTo
title: Use CommunityToolkit IoC
tags: [dependency-injection, toolkit, ioc]
---
# Resolve services through CommunityToolkit IoC

Expose the Uno Extensions service provider through `CommunityToolkit.Mvvm` when you need to resolve dependencies outside of constructor injection.

> [!WARNING]
> Prefer constructor injection wherever possible. Manual resolution is a temporary bridge while refactoring existing code.

## Enable the toolkit IoC bridge

Add the `Mvvm` Uno Feature so `CommunityToolkit.Mvvm` is referenced.

```diff
<UnoFeatures>
    Material;
    Extensions;
+   Mvvm;
    Toolkit;
</UnoFeatures>
```

## Register your services as usual

Configure the host with your service registrations.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.ConfigureServices(services =>
                services.AddSingleton<IPrimaryService, PrimaryService>()
                        .AddSingleton<ISecondaryService, SecondaryService>());
        });

    Host = builder.Build();
}
```

## Expose the container via `Ioc.Default`

After building the host, wire the service provider into the toolkit IoC.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    // host setup...
    Host = builder.Build();
    Ioc.Default.ConfigureServices(Host.Services);
}
```

`Ioc.Default` now delegates to the same container that Uno Extensions uses.

## Resolve services where injection is not yet available

Leverage `GetService`/`GetRequiredService` inside legacy view models or helpers.

```csharp
public class MainViewModel : ObservableRecipient
{
    private readonly IPrimaryService? _primaryService =
        Ioc.Default.GetService<IPrimaryService>();

    private readonly ISecondaryService _secondaryService =
        Ioc.Default.GetRequiredService<ISecondaryService>();
}
```

Use `GetRequiredService` when the dependency must exist, and `GetService` when you can tolerate null.

## Resources

- [Dependency injection in Uno Extensions](xref:Uno.Extensions.DependencyInjection.HowToDependencyInjection)
- [CommunityToolkit.Mvvm IoC](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/ioc)
