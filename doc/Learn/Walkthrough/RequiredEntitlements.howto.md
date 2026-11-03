---
uid: Uno.Extensions.Storage.RequiredEntitlements.HowTo
title: Enable Keychain Entitlements
tags: [storage, ios, entitlements]
---
# Enable keychain access for Uno storage

Uno storage uses the Apple Keychain on iOS. To persist secrets successfully you must configure the `keychain-access-groups` entitlement and align it with your provisioning profile.

## Create `Entitlements.plist`

Add an entitlements file to the iOS platform folder of your project:

- `iOS/Entitlements.plist`

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
<PropertyGroup Condition="$(IsIOS)">
  <CodesignEntitlements>iOS\Entitlements.plist</CodesignEntitlements>
  <CodesignKey>Apple Development: Jane Doe (XXXXXXXXXX)</CodesignKey>
  <CodesignProvision>iOS Team Provisioning Profile: com.companyname.myapp</CodesignProvision>
</PropertyGroup>
```

Use the exact signing identity and provisioning profile names from the developer portal or from the Xcode build logs.

## Rebuild and verify

After updating entitlements and signing, rebuild the app for iOS. If the configuration is correct, the app launches and the storage extension can read/write secrets from the Keychain. If signing fails, check the system logs via Apple’s Console.app for entitlement or profile errors.

## Resources

- [Apple Keychain access groups](https://developer.apple.com/documentation/bundleresources/entitlements/keychain-access-groups)
- [Microsoft .NET entitlements (iOS)](https://learn.microsoft.com/dotnet/maui/ios/entitlements)
