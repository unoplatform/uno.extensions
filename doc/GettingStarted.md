---
uid: Uno.Extensions.HowToGettingStarted
---
# How-To: Getting Started

This tutorial will walk you through how to create an Uno application with Uno.Extensions using the Wizard and CLI and how to install Uno.Extensions to an existing Uno application.

> [!NOTE]
> Make sure to setup your environment first by [following our instructions](xref:Uno.GetStarted.vs2022).

## Creating a new application

### 1. Creating the app

### [Wizard](#tab/wizard)

#### Create an Uno Platform app with Wizard

* Create a new C# solution using the **Uno Platform App** template, from Visual Studio's **Start Page**, then click the **Next** button

    ![Visual Studio - Get started - Selecting `create a new project` option](./Learn/images/newproject1.png)
    ![Visual Studio - Create a new project - Selecting `Uno Platform App` option](./Learn/images/newproject2.png)

* Configure your new project by providing a project name and a location, check the "**Place solution and project in the same directory**" option, then click the **Create** button

    ![Visual Studio - Configure project name and location](./Learn/images/configure-new-unoplatform-app.png)

* Choose the **Recommended** preset to build your application

    ![Visual Studio - Configure your new project](./Learn/images/intro.png)

    > [!TIP]
    > For a detailed overview of the Uno Platform project template wizard and all its options, see [this](xref:Uno.GettingStarted.UsingWizard).

* Click the create button

* Wait for the projects to be created, and their dependencies to be restored

* A banner at the top of the editor may ask to reload projects, click **Reload projects**:  
    ![Visual Studio - A banner indicating to reload projects](./Learn/images/vs2022-project-reload.png)

### Detect problems with Uno Check directly in your IDE

Another banner may appear, telling you Uno Check found problems, click **Fix**

![Visual Studio - A banner indicating to fix problems](./Learn/images/vs2022-uno-check-banner.png)

Now it will open a Terminal and run the check again, so you can see what exact problem it may have found and let it fix them for you.

#### [Using the Command Line](#tab/cli)

The `dotnet` templates included in the `Uno.Templates` package are used to easily create new projects that already reference the Uno.Extensions.

* Open a command prompt and run the following:

    ```dotnetcli
    dotnet new install Uno.Templates
    ```

* Navigate to the desired projects directory, and use the `unoapp` template to generate the starter solution discussed above

    ```dotnetcli
    dotnet new unoapp -o MyProject -preset recommended
    ```

    The argument specified after the `-o` flag (i.e. MyProjectName) will act as the name for both a containing directory and the generated solution.

* Open the solution in Visual Studio

    `.\MyProjectName\MyProjectName.sln`

---

### 2. Exploring the Solution

The generated solution will contain *MyProjectName* for application logic, including constructs like ViewModels and services, along with the pages, controls, and other views constituting the UI of the application.

![The structure of the generated solution](./Learn/images/ProjectStructure-min.png)

### 3. Running the Application

* Select a target from the drop-down as pictured below

    ![A screenshot of the generated targets](./Learn/images/GeneratedTargets-min.png)

* Click the "play" button, or press F5 to start debugging. The project will be compiled and deployed based on the target platform. For more detailed instructions specific to each platform, refer to the [Debug the App](xref:Uno.GettingStarted.CreateAnApp.VS2022#debug-the-app) documentation.

## Installing Extensions in an existing project

To get started with Extensions in your project, follow these steps:

### Step 1: Add Hosting to Your Project

Hosting is the foundation for using Extensions. Begin by adding Hosting to your project. Refer to the detailed instructions in the [Hosting Setup Documentation](xref:Uno.Extensions.Hosting.HowToHostingSetup).

After this, add a `protected` property named Host of type `IHost` to your `App.xaml.cs` file below the MainWindow Property:

```csharp
protected Window? MainWindow { get; private set; }
protected IHost? Host { get; private set; }
```

### Step 2: Configure the OnLaunched Method

After setting up Hosting, adjust the `OnLaunched` method in `App.xaml.cs` to initialize the Extensions features. Ensure you have added the necessary [Uno Platform Features](xref:Uno.Features.Uno.Sdk#uno-platform-features).

Create or Upgrade the `HostBuilder` depending on your desired Extensions:

```csharp
var builder = this.CreateBuilder(args)
    // Add navigation support for toolkit controls such as TabBar and NavigationView
    .UseToolkitNavigation()
    // Configure the host builder
    .Configure(host => host
#if DEBUG
        // Switch to Development environment when running in DEBUG
        .UseEnvironment(Environments.Development)
#endif
        // Get Read and Write functions for Package Files
        .UseStorage()
        .UseLogging(configure: (context, logBuilder) =>
        {
            // Configure log levels for different categories of logging
            logBuilder
                .SetMinimumLevel(
                    context.HostingEnvironment.IsDevelopment() ?
                        LogLevel.Information :
                        LogLevel.Warning)

                // Default filters for core Uno Platform namespaces
                .CoreLogLevel(LogLevel.Warning);

            // Uno Platform namespace filter groups
            // Uncomment individual methods to see more detailed logging

            //// Generic Xaml events
            //logBuilder.XamlLogLevel(LogLevel.Debug);
            //// Layout specific messages
            //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
            //// Storage messages
            //logBuilder.StorageLogLevel(LogLevel.Debug);
            //// Binding related messages
            logBuilder.XamlBindingLogLevel(LogLevel.Debug);
            //// Binder memory references tracking
            logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
            //// DevServer and HotReload related
            //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
            //// Debug JS interop
            //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

        }, enableUnoLogging: true)
        .UseConfiguration(configure: configBuilder =>
            configBuilder
                .EmbeddedSource<App>()
                .Section<AppConfig>()
                // Add future Sections that should be read from `appsettings.json` file here
        )
        // Enable localization (see appsettings.json and Package.appxmanifest to define your currently supported languages)
        .UseLocalization()
        // Register Json serializers (ISerializer and ISerializer)
        .UseSerialization((context, services) => services
            .AddContentSerializer(context)
        .UseHttp((context, services) => services
            // Register HttpClient
#if DEBUG
            // DelegatingHandler will be automatically injected into Refit Client
            .AddTransient<DelegatingHandler, DebugHttpHandler>()
#endif
        .ConfigureServices((context, services) =>
            services
                // TODO: Register your regular services
                .AddSingleton<ISampleService, SampleService>()
        )
        // Enable Uno.Extensions.Navigation and add this content in case you use MVUX
        .UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)
    );
```

### Step 3: Use the Builder to Create the Main Window

#### [Using Frame Navigation](#tab/frame-navigation)

Finally, instead of directly creating an instance of a `Window` using `MainWindow = new Window()`, use the `builder` to set up the main window:

```diff
-MainWindow = new Window();

var builder = this.CreateBuilder(args)
    .Configure(host => host
        // Configure the host builder
    );

+MainWindow = builder.Window;

Host = builder.Build();

if (MainWindow.Content is not Frame rootFrame)
{
    rootFrame = new Frame();
    MainWindow.Content = rootFrame;
}

if (rootFrame.Content == null)
{
    rootFrame.Navigate(typeof(MainPage), args.Arguments);
}

MainWindow.Activate();
```

#### [Using Region Navigation](#tab/uno-extensions-navigation)

Finally, instead of directly creating an instance of a `Window` using `MainWindow = new Window()`, use the `builder` to set up the main window:

```diff
-   MainWindow = new Window()
+   MainWindow = builder.Window;

+   Host = await builder.NavigateAsync<Shell>();
}
```

---

> [!IMPORTANT]
> Be sure to remove any other code that sets `MainWindow` to prevent conflicts in your application.

---

[!INCLUDE [getting-help](./Learn/includes/getting-help.md)]
