---
uid: Learn.Tutorials.Authentication.HowToAuthentication
---
# How-To: Get Started with Authentication

`Uno.Extensions.Authentication` provides you with a consistent way to add authentication to your application. It is recommended to one of the built in `IAuthenticationService` implementations.

## Step-by-steps

### 1. Specify configuration information to load on `IConfigBuilder`

* Uno.Extensions apps specify which configuration information to load by calling the `UseConfiguration()` extension method for `IHostBuilder`.

* Use the `EmbeddedSource<T>()` extension method on `IConfigBuilder` to load configuration information from an assembly type you specify:

    ```csharp
    public App()
    {
        Host = UnoHost
            .CreateDefaultBuilder()
            .UseConfiguration(configure: configBuilder =>
                configBuilder
                    // Load configuration information from appsettings.json
                    .EmbeddedSource<App>()
            )
    ...
    ```

* By default, this method will extract values from an embedded resource (using the [EmbeddedResource](https://docs.microsoft.com/en-us/dotnet/api/system.codedom.compiler.compilerparameters.embeddedresources?view=dotnet-plat-ext-6.0#remarks) file build action) called `appsettings.json`, unless you optionally denote a different file name. The string you pass into the extension method will be concatenated in-between `appsettings` and its file extension. For instance, the following will also retrieve values from the file `appsettings.platform.json` embedded inside the `App` assembly:

    ```csharp
    public App()
    {
        Host = UnoHost
            .CreateDefaultBuilder()
            .UseConfiguration(configure: configBuilder =>
                configBuilder
                    .EmbeddedSource<App>()
                    // Load configuration information from appsettings.platform.json
                    .EmbeddedSource<App>("platform")
            )
    ...
    ```

### 2. Define a class to model the configuration section

* Your JSON file(s) will consist of a serialized representation of multiple properties and their values. Hence, configuration sections allow you to programmatically read a specific subset of these properties from the instantiated class that represents them.

* Author a new class or record with related properties to be used for configuration:

    ```csharp
    public record Auth
    {
        public string? ApplicationId { get; init; }
        public string[]? Scopes { get; init; }
        public string? RedirectUri { get; init; }
        public string? KeychainSecurityGroup { get; init; }
    }
    ```

### 3. Load a specific configuration section

* You can now use the `Section<T>()` extension method on `IConfigBuilder` to load configuration information for class or record of the type argument you specify:

    ```csharp
    public App()
    {
        Host = UnoHost
            .CreateDefaultBuilder()
            .UseConfiguration(configure: configBuilder =>
                configBuilder
                    // Load configuration information from appsettings.json
                    .EmbeddedSource<App>()
                    // Load configuration information from appsettings.platform.json
                    .EmbeddedSource<App>("platform")
                    // Load Auth configuration section
                    .Section<Auth>()
            )
    ...
    ```

### 4. Read configuration section values from a registered service

* To access the instantiated representation of the configuration section you registered above, complete with values populated from the `appsettings.json` file, you'll need to add a new constructor parameter for it to one of your application's services.

* The configuration section will be injected as an object of type `IOptions<T>`, so add a corresponding parameter for it to the constructor of the service:

    ```csharp
    public class AuthenticationService : IAuthenticationService
    {        
        public AuthenticationService(IOptions<Auth> settings)
        {
            var authSettings = settings.Value;
    ...
    ```
