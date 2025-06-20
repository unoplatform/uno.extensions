---
uid: Uno.Extensions.Storage.Overview
---

# Storage

> **UnoFeatures:** `Storage` (add to `<UnoFeatures>` in your `.csproj`)

Uno.Extensions.Storage facilitate local data storage across multiple platforms, including WebAssembly, Android, iOS, macOS, Desktop and Windows. This extension is particularly useful for applications that require secure, persistent storage of user preferences, configuration settings, and sensitive information such as tokens and credentials.

> [!IMPORTANT]
> On Apple platforms (iOS, Mac Catalyst) the Uno storage extension, used by the authentication extension, uses the OS Key Chain service to store secrets. This requires your application to have the [proper entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements) to work properly.

Additionally, it enables to provide a own `ISerializer` from [Uno.Extensions.Serialization](xref:Uno.Extensions.Serialization.Overview) if your Data or File format is not supported out of the box.

## Explore the Storage Feature

- **Start here:** [Getting Started with Storage](xref:Uno.Extensions.Storage.GettingStarted)
- Handling Package Files
- [How to: Required Entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements)

## Which files could be preferred to use Storage instead of `appsettings.json` via `IConfiguration`

As simple as it sounds, you can use the Storage feature to read and write files (*almost*) independent of their type, while `appsettings.json` is a JSON file that is used to store application settings and configuration data.

> [!TIP]
> Here is a rule for Storage Package files to remember:
>
> As long as you can provide a [ISerializer](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Serialization/ISerializer.cs) or your file is generally readable as `string` or `Stream`, this can be handled with Uno.Extensions.Storage.
> [!NOTE]
> For other files that have their BuildAction set to Resources or Content, these can be referenced via `ms-appx:///Here/comes/your/filepath.txt` to read the file from the package.

## Next steps

Check out the [Getting Started Guide](xref:Uno.Extensions.Storage.GettingStarted)

## See Also

- [Uno.UI.Toolkit.StorageFileHelper](https://platform.uno/docs/articles/features/file-management.html)
- Uno specifics for [File and Folder Pickers](https://platform.uno/docs/articles/features/windows-storage-pickers.html)
- Storing and reading [Settings](https://platform.uno/docs/articles/features/settings.html)

---

[!INCLUDE [getting-help](./includes/getting-help.md)]
