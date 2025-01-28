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

[!include[existing-app](../includes/existing-app.md)]

[!include[single-project](../includes/single-project.md)]

For more information about `UnoFeatures` refer to our [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk) docs.
