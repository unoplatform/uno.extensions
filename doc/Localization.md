# Localization
_[TBD - Review and update this guidance]_

We use [Microsoft.Extensions.Localization](https://www.nuget.org/packages/Microsoft.Extensions.Localization) for any configuration related work.

For more documentation on localization, read the references listed at the bottom.

## Text localization

- We use `.resw` files located under the [Strings folder](../src/app/ApplicationTemplate.Shared/Strings) to localize texts.

- An Uno implementation of `IStringLocalizer` (`ResourceLoaderStringLocalizer`) is registered as service in the [LocalizationConfiguration.cs](../src/app/ApplicationTemplate.Shared/Configuration/LocalizationConfiguration.cs) file.

- We use `IStringLocalizer` to resolve those localized texts
  ```csharp
  var stringLocalizer = serviceProvider.GetService<IStringLocalizer>();
  
  // Using IStringLocalizer as a dictionary
  string myString = stringLocalizer["MyKey"];

  // You can get a LocalizedString object too
  LocalizedString myString = stringLocalizer["MyKey"];
  var isResourceNotFound = myString.ResourceNotFound;
  ```

## UI Culture

- In most cases, the **system UI culture** will define the displayed culture of the application; this is the default behaviour. 

- In some cases, we would like to allow the user to switch to another language based on a user preference (sandboxed to the application). This template offers this ability using a configuration setting (`ThreadCultureOverrideService`) configured in the [LocalizationConfiguration.cs](../src/app/ApplicationTemplate.Shared/Configuration/LocalizationConfiguration.cs) file.

  - Much like the [runtime environment](Environments.md), the preferred culture is saved in a file and processed during the startup. We use cultures instead of languages to support different versions of the same language (e.g. en-US vs en-UK or fr-CA vs fr-FR).

  - Once the culture is resolved, the thread culture is overwritten using the new culture. If the user culture is not supported by the application, the default culture will be used instead.

  - You can expose this feature in a settings page for example (toggle between English and French).

    ```csharp
    var threadCultureOverrideService = serviceProvider.GetService<ThreadCultureOverrideService>();
    threadCultureOverrideService.SetCulture(new CultureInfo("fr-CA"));
    ```

## Diagnostics

Multiple localization features can be tested from the diagnostics screen. This is configured in [SummaryDiagnosticsViewModel](../src/app/ApplicationTemplate.Shared/Presentation/Diagnostics/CultureDiagnosticsViewModel.cs).

- You can see the current UI culture.
- You can set another UI culture (supported or not by the application); this is very useful to test how the application will behave in an unsupported culture. 

## References

- [Using IStringLocalizer](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-3.1)