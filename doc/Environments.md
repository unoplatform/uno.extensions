# Environments
_[TBD - Review and update this guidance]_

We use [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting) for any environment related work.

For more documentation on environments, read the references listed at the bottom.

## Runtime environments

By default, the template offers the following runtime environments, defined by their corresponding `appsettings` files.

- Development (`appsettings.development.json`)
- Staging (`appsettings.staging.json`)
- Production (`appsettings.production.json`)

You can add / remove runtime environments by simply adding or removing appsettings files (e.g. `appsettings.myenvironment.json` will create a new runtime environment named `myenvironment`).

This is configured inside the [AppSettingsConfiguration.cs](../src/app/ApplicationTemplate.Shared/Configuration/AppSettingsConfiguration.cs) file.

- The default runtime environment is set based on a compile-time directive (e.g. production).

- You can get the current environment using `AppSettingsConfiguration.AppEnvironment.GetCurrent`.

- You can get all the possible environments using `AppSettingsConfiguration.AppEnvironment.GetAll`.

- You can set the current environment using `AppSettingsConfiguration.AppEnvironment.SetCurrent`. If the environment doesn't exist, you will get an exception.

- The current environment is persisted into a file that is processed at startup; this allows the environment to affect your IoC and be faster than accessing any settings service (e.g. no IoC, no deserialization, etc.).

- The current environment is also used when configuring the `IHostBuilder`. For example, you can do the following:
  ```csharp
  hostBuilder.ConfigureServices((context, services) => {
    var isDevelopment = context.HostingEnvironment.IsDevelopment();
    var isStaging = context.HostingEnvironment.IsStaging();
    var isProduction = context.HostingEnvironment.IsProduction();
    var environment = context.HostingEnvironment.EnvironmentName;
    var isMyEnvironment = context.HostingEnvironment.IsEnvironment("MyEnvironment");
  });
  ```

## Compile-time environments

Compile-time environments should be avoided as much as possible. It's always preferable to properly support runtime environments. Compile-time environments require a different compilation of the code which is affected most of the time using compilation directives (e.g. `#if PRODUCTION`) and constants.

Using compile-time environments...

- Complexifies the release management.
- Doubles the build / release process.
- Doesn't support complete switching of environments at runtime.
- (a lot more)

At the moment, the template includes the `PRODUCTION` and `STAGING` compile-time environments. This directive is used only to set the default environment (e.g. production instead of development).

This directive is declared during the build process using an MSBuild argument (`ApplicationEnvironment`) processed in the [Directory.Build.props](../Directory.Build.props) file.

The `ApplicationEnvironment` argument is passed to MSBuild in the [build steps](../build/steps-build.yml).

**IMPORTANT: Your code should behave differently based ONLY on the runtime environment and NOT the compile-time environment. This allows you to have a much better support of environment switching.**

```csharp
// Don't do this
#if PRODUCTION
var myResult = MyApi.GetAll("myapi-prod/results");
#else
var myResult = MyApi.GetAll("myapi-dev/results");
#endif

// Do this
if (GetRuntimeEnvironment() == "production") { ... }
```

## Diagnostics

Multiple environment features can be tested from the diagnostics screen. This is configured in [SummaryDiagnosticsViewModel](../src/app/ApplicationTemplate.Shared/Presentation/Diagnostics/SummaryDiagnosticsViewModel.cs).

- You can see the current runtime environment. 
- You can switch to another runtime environment.

## References