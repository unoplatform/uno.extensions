---
uid: Uno.Extensions.Hosting.HowToHostingSetup
---
# How-To: Get Started with Hosting

`Uno.Extensions.Hosting` can be used to register services that will be accessible throughout the application via dependency injection (DI). This tutorial will walk you through the critical steps needed to leverage hosting in your application.

> [!WARNING]
> The steps outlined here are unnecessary if you used the new project wizard template to create your solution. Otherwise, it is recommended that you follow the [instructions](xref:Overview.Extensions) for creating an application from the template.

## Step-by-steps

### 1. Installation
* Install the [Uno.Extensions.Hosting.WinUI](https://www.nuget.org/packages/Uno.Extensions.Hosting.WinUI) package from NuGet.

### 2. Create and Configure IApplicationBuilder

* We need to expose the `IHost` instance to the rest of App.cs. Add the following property to your class file:
    ```cs
    private IHost Host { get; }
    ```

* As soon as your app is launched, use the `CreateBuilder()` extension method to instantiate an `IApplicationBuilder` from your `Application` object:
    ```cs
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            // Configure the host builder
        });
    ...
    ```

### 3. Build the IHost

* Finally, build the host and assign it to the `Host` property:
    ```cs
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            // Configure the host builder
        });

        Host = appBuilder.Build();
    ...
    ```