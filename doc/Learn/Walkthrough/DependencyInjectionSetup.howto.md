---
uid: Uno.Extensions.DependencyInjection.DependencyInjection.HowTo
title: Register and Consume Services
tags: [dependency-injection, services]
---
# Register and consume services through dependency injection

Define interfaces, register implementations during host configuration, and inject them into view models the Uno Extensions way.

> [!IMPORTANT]
> Ensure hosting is configured first. Follow [Hosting setup](xref:Uno.Extensions.Hosting.HowToHostingSetup) if your app was created without it.

## Describe the service contract

Start with an interface that expresses the behavior you want to share.

```csharp
public interface IProfilePictureService
{
    Task<byte[]> GetAsync(CancellationToken ct);
}
```

## Implement the service

Provide the concrete logic in a class that implements the interface.

```csharp
public class ProfilePictureService : IProfilePictureService
{
    public async Task<byte[]> GetAsync(CancellationToken ct)
    {
        // call backend or disk, then return bytes
    }
}
```

## Register services during host configuration

Add your service registrations inside the host builder.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.ConfigureServices(services =>
            {
                services.AddSingleton<IProfilePictureService, ProfilePictureService>();
                services.AddTransient<MainViewModel>(); // optional when not using navigation
            });
        });
}
```

Choose the lifetime (`AddSingleton`, `AddTransient`, etc.) that matches how the service should behave.

## Consume services through constructor injection

Request the interface in your view model’s constructor and store the dependency.

```csharp
public class MainViewModel
{
    private readonly IProfilePictureService _profileService;

    public MainViewModel(IProfilePictureService profileService)
    {
        _profileService = profileService;
    }

    public async Task<byte[]> LoadPhotoAsync(CancellationToken ct) =>
        await _profileService.GetAsync(ct);
}
```

The container resolves the implementation automatically when the view model is instantiated.

## Connect the view model to the view

If you are not using navigation, manually resolve the view model and assign it as the `DataContext`.

```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        DataContext = ((App)Application.Current).Host.Services
            .GetRequiredService<MainViewModel>();
    }
}
```

Navigation-connected views receive their view models automatically, so explicit resolution is typically unnecessary in MVUX or Navigation-based setups.

## Resources

- [Hosting setup](xref:Uno.Extensions.Hosting.HowToHostingSetup)
- [Navigation overview](xref:Uno.Extensions.Navigation.Overview)
