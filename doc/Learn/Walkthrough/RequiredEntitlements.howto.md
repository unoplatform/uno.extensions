---
uid: Uno.Extensions.Storage.RequiredEntitlements
title: Enable Keychain Entitlements
tags: [storage, ios, mac-catalyst, entitlements]
---
# Enable keychain access for Uno storage

Uno storage uses the Apple Keychain on iOS and Mac Catalyst. To persist secrets successfully you must configure the `keychain-access-groups` entitlement and align it with your provisioning profile.

## Create `Entitlements.plist`

Add an entitlements file per Apple platform in your solution:

- `iOS/Entitlements.plist`
- `MacCatalyst/Entitlements.plist`

```xml
<key>keychain-access-groups</key>
<array>
    <string>$(AppIdentifierPrefix)$(CFBundleIdentifier)</string>
</array>
```

The build pipeline replaces `$(AppIdentifierPrefix)` and `$(CFBundleIdentifier)` at compile time, so you normally do not hard-code values.

## Add the capability in Apple Developer portal

Entitlements must also be enabled server-side. In the Apple Developer portal:

1. Open Certificates, IDs & Profiles.
2. Locate your app identifier.
3. Enable **Keychain Sharing** and regenerate the provisioning profile.

Alternatively, create an Xcode project with the same bundle identifier, add the capability under **Signing & Capabilities**, and let Xcode update the managed profile for you.

## Reference the entitlements during build

Update your `.csproj` (typically `*.Mobile.csproj`) with the entitlements path and signing details.

```xml
<PropertyGroup>
  <CodesignEntitlements Condition="$(IsIOS)">iOS\Entitlements.plist</CodesignEntitlements>
  <CodesignEntitlements Condition="$(IsMacCatalyst)">MacCatalyst\Entitlements.plist</CodesignEntitlements>
  <CodesignKey>Apple Development: Jane Doe (XXXXXXXXXX)</CodesignKey>
  <CodesignProvision>Mac Catalyst Team Provisioning Profile: com.companyname.maccatalyst</CodesignProvision>
</PropertyGroup>
```

Use the exact signing identity and provisioning profile names from the developer portal or from the Xcode build logs.

## Rebuild and verify

After updating entitlements and signing, rebuild the app for iOS or Mac Catalyst. If the configuration is correct, the app launches and the storage extension can read/write secrets from the Keychain. If signing fails, check the system logs via Apple’s Console.app for entitlement or profile errors.

## Resources

- [Apple Keychain access groups](https://developer.apple.com/documentation/bundleresources/entitlements/keychain-access-groups)
- [Microsoft .NET entitlements (iOS)](https://learn.microsoft.com/dotnet/maui/ios/entitlements)
- [Microsoft .NET entitlements (Mac Catalyst)](https://learn.microsoft.com/dotnet/maui/mac-catalyst/entitlements)
