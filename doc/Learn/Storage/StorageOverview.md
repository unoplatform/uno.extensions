---
uid: Uno.Extensions.Storage.Overview
---

# Storage

Uno.Extensions.Storage facilitate local data storage across multiple platforms, including WebAssembly, Android, iOS, macOS, Desktop and Windows. This extension is particularly useful for applications that require secure, persistent storage of user preferences, configuration settings, and sensitive information such as tokens and credentials.

> [!IMPORTANT]
> On Apple platforms (iOS, Mac Catalyst) the Uno storage extension, used by the authentication extension, uses the OS Key Chain service to store secrets. This requires your application to have the [proper entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements) to work properly.

## Installation

`Storage` is provided as an Uno Feature. To enable `Storage` support in your application, add `Storage` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

```diff
<UnoFeatures>
    Material;
    Extensions;
+   Storage;
    Toolkit;
    MVUX;
</UnoFeatures>
```

> [!IMPORTANT]
> This walkthrough assumes you created your app using the Single Project template. If you used a different template, please refer to our [Migrating Projects to Single Project](xref:Uno.Development.MigratingToSingleProject) documentation.

For more information about `UnoFeatures` refer to our [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk) docs.
