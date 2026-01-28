---
uid: Uno.Extensions.Storage.Overview
---

# Storage

> **UnoFeatures:** `Storage` (add to `<UnoFeatures>` in your `.csproj`)

Uno.Extensions.Storage facilitate local data storage across multiple platforms, including WebAssembly, Android, iOS, macOS, Desktop and Windows. This extension is particularly useful for applications that require secure, persistent storage of user preferences, configuration settings, and sensitive information such as tokens and credentials. Additionally, it provides an `ISerializer` from [Uno.Extensions.Serialization](xref:Uno.Extensions.Serialization.Overview) to return the deserialized type your code needs.

> [!IMPORTANT]
> On Apple platforms (iOS, Mac Catalyst) the Uno storage extension, used by the authentication extension, uses the OS Key Chain service to store secrets. This requires your application to have the [proper entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements) to work properly.

## Getting Started

[!INCLUDE [single-project](../includes/single-project.md)]

[!INCLUDE [create-application](../includes/create-application.md)]

### 1. Prepare for Storage

- Add `Storage` to the `<UnoFeatures>` property in your .csproj file.

    ```diff
    <UnoFeatures>
    +   Storage;
    </UnoFeatures>
    ```

### 2. Set up Storage

- Use the `UseStorage()` extension method to configure the `IHostBuilder` to enable Storage.

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host =>
            {
                host.UseStorage();
            });
        ...
    }
    ```

### 3. Inject IStorage into your view model

- Inject `IStorage` into your view model's constructor to enable dependency injection.

    ```csharp
    public partial class MainViewModel
    {
        private readonly IStorage _storage;

        public MainViewModel(IStorage storage)
        {
            _storage = storage;
        }
    }
    ```

### 4. Add files to your project

- Add the file(s) you want to read or write to in your app.

- Open your .csproj file and add the following to the `<ItemGroup>` section:

    ```xml
    <ItemGroup>
        <Resource Include="Assets\<YourFolderNameIfAny>\*" CopyToOutputDirectory="PreserveNewest" />
    </ItemGroup>
    ```

## Additional Resources

- [How to: Required Entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements)

## See Also

- [Uno.UI.Toolkit.StorageFileHelper](https://platform.uno/docs/articles/features/file-management.html)
- Uno specifics for [File and Folder Pickers](https://platform.uno/docs/articles/features/windows-storage-pickers.html)
- Storing and reading [Settings](https://platform.uno/docs/articles/features/settings.html)

---

[!INCLUDE [getting-help](./includes/getting-help.md)]
