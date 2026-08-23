---
uid: Uno.Extensions.Storage.Overview
---

# Storage

> **UnoFeatures:** `Storage` (add to `<UnoFeatures>` in your `.csproj`)

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

## Key-value storage

`UseStorage()` registers a default `IKeyValueStorage` per platform. Everything built on top of it — most visibly the authentication token cache — writes through this default:

| Platform | Default store |
| --- | --- |
| Windows (WinAppSDK) | `ApplicationData` settings, DPAPI-encrypted |
| Android (native renderer) | `KeyStore` |
| iOS (native renderer) | Keychain |
| Android / iOS with the Skia renderer | `ApplicationData` settings (unencrypted, inside the app sandbox) — see the note below |
| WebAssembly | Browser storage — `localStorage` by default, see below |
| Other (e.g. Skia Desktop) | `ApplicationData` settings (unencrypted file) |

> [!NOTE]
> On Android and iOS heads built with `UnoFeatures=SkiaRenderer`, the Uno SDK loads this package's plain `netX.0` build rather than its `netX.0-android` / `netX.0-ios` one (the same substitution it applies to every Uno.UI-referencing package), and the `KeyStore` / Keychain stores only exist in the platform builds. The default there is therefore `ApplicationData`, which the OS sandboxes per app but does not encrypt. Register your own `IKeyValueStorage` as the default if you need more than the sandbox on those heads.

### WebAssembly: choosing the browser store

The browser has no protected store, so on WebAssembly the choice is about *lifetime*, not encryption. Configure it in the storage section:

```json
{
  "KeyValueStorageConfiguration": {
    "BrowserCacheLocation": "LocalStorage"
  }
}
```

| Value | Behavior |
| --- | --- |
| `LocalStorage` (default) | Survives a page reload, closing the tab, and restarting the browser. This is what WebAssembly used before the setting existed, so upgrading never relocates an app's existing data. |
| `SessionStorage` | Survives a page reload; cleared when the tab closes. The tightest persistent lifetime — worth opting into for anything holding credentials. |
| `MemoryStorage` | Nothing is written to browser storage; values live for the lifetime of the page only. |

An invalid value throws while the host is being built rather than silently falling back — deliberate for a setting that decides where credentials live. Numeric values are rejected too. The setting is ignored on every other platform.

Because this selects the single host-wide default store, it also governs the authentication token cache — whatever the authentication provider is, and whatever name it was registered under. For the security implications when tokens are involved (MSAL, refresh-token lifetime, the Entra `spa` registration), see [MSAL Authentication](xref:Uno.Extensions.Authentication.HowToMsalAuthentication).
