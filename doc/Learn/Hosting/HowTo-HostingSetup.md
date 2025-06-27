---
uid: Uno.Extensions.Hosting.HowToHostingSetup
---
# How-To: Get Started with Hosting

`Hosting` can be used to register services that will be accessible throughout the application via dependency injection (DI). This tutorial will walk you through the critical steps needed to leverage hosting in your application.

[!include[create-application](../includes/create-application.md)]

## Step-by-step

### 1. Installation

* Add `Hosting` to the `<UnoFeatures>` property in the Class Library (.csproj) file. If you already have `Extensions` in `<UnoFeatures>`, then `Hosting` is already installed, as its dependencies are included with the `Extensions` feature.

    ```diff
    <UnoFeatures>
        Material;
    +   Hosting;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

### 2. Create and Configure IApplicationBuilder

* We need to expose the `IHost` instance to the rest of App.cs. Add the following property to your class file:

    ```cs
    private IHost Host { get; set; }
    ```

  The `IHost` type is defined in the `Microsoft.Extensions.Hosting` namespace. Since this and other related types will be used throughout this guide, it's recommended to add this namespace to your `GlobalUsings.cs` file for convenience and cleaner code.

  ```csharp
  global using Microsoft.Extensions.Hosting;
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
    }
    ```

### 3. Set Up the Main Window with the Builder

* Replace `MainWindow = new Window()` or any `MainWindow` assignment with the `builder` approach:

    ```cs
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            // Configure the host builder
        });

        MainWindow = builder.Window;
        ...
    }
    ```

### 4. Build the IHost

* Finally, build the host and assign it to the `Host` property:

    ```cs
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            // Configure the host builder
        });

        MainWindow = builder.Window;

        Host = appBuilder.Build();
        ...
    }
    ```
