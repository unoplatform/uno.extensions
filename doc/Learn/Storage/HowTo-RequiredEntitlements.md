---
uid: Uno.Extensions.Storage.HowToRequiredEntitlements
---
# How-To: Add Required Entitlements

On Apple platforms (iOS, Mac Catalyst) the Uno storage extension uses the OS Key Chain service to store secrets. This requires your application to have the [`keychain-access-groups`](https://developer.apple.com/documentation/bundleresources/entitlements/keychain-access-groups) entitlement to work properly.

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Add the Entitlements.plist file

The default location, inside your project, for the new file(s) should be:

* iOS: `iOS/Entitlements.plist`
* Mac Catalyst: `MacCatalyst/Entitlements.plist`

The content of the file(s) should be:

```xml
<key>keychain-access-groups</key> 
 <array> 
     <string>$(AppIdentifierPrefix)$(CFBundleIdentifier)</string> 
 </array>
```

For more information see Apple's documentation related to the [Key Chain](https://developer.apple.com/documentation/security/keychain_services/keychain_items/sharing_access_to_keychain_items_among_a_collection_of_apps?language=objc).

The variables `$(AppIdentifierPrefix)` and `$(CFBundleIdentifier)` will be replaced with the correct values at build time. For more information about how the Microsoft .NET SDK works with entitlements you can consult:

* [Microsoft iOS](https://learn.microsoft.com/en-us/dotnet/maui/ios/entitlements)
* [Microsoft Mac Catalyst](https://learn.microsoft.com/en-us/dotnet/maui/mac-catalyst/entitlements)

### 2. Add capabilities in your Apple Developer Account

Adding the `Entitlements.plist` to your project is not enough. You must also add the capability inside your Apple Developer Account and create a provisioning profile specific for your application. You can follow Microsoft's instructions for both steps:

* [Microsoft iOS](https://learn.microsoft.com/en-us/dotnet/maui/ios/capabilities?#add-capabilities-in-your-apple-developer-account)
* [Microsoft Mac Catalyst](https://learn.microsoft.com/en-us/dotnet/maui/mac-catalyst/capabilities?#add-capabilities-in-your-apple-developer-account)

> [!NOTE]
> You can use XCode to create a project, go to the **Signing and Capabilities**, use the same bundle identifier, add the **Keychain Sharing** capacity (again using the same bundle identifier) then ask Xcode to _fix_ your `Xcode Managed Profile`.

### 3. Modifying the `*.Mobile.csproj`

A new property group should be added to your `*.csproj` project file. The example below will work for both iOS and Mac Catalyst targets.

```xml
<PropertyGroup>
    <CodesignEntitlements Condition="$(IsIOS)">iOS\Entitlements.plist</CodesignEntitlements>
    <CodesignEntitlements Condition="$(IsMacCatalyst)">MacCatalyst\Entitlements.plist</CodesignEntitlements>
    <CodesignKey>Apple Development: Some User (XXXXXXXXXX)</CodesignKey>
    <CodesignProvision>Mac Catalyst Team Provisioning Profile: com.companyname.maccatalyst</CodesignProvision>
</PropertyGroup>
```

The values for the `CodesignKey` and `CodesignProvision` **must** match the values from the [Apple Developer Portal](https://developer.apple.com/account).

> [!NOTE]
> If you used Xcode earlier then build the application and get both values from the build logs.

### 4. Rebuilding your application

Finally rebuilding the application for your target(s) will now code sign your application. This makes the entitlements valid and allows the Key Chain API to work properly at runtime.

### 5. Troubleshooting

Code signing issues can be difficult to diagnose as the application won't start (or hang) if misconfigured. The operating systems (both iOS or macOS) will log code signing failures. You can see the logs by using Apple's [Console.app](https://support.apple.com/en-ca/guide/console/welcome/mac).
