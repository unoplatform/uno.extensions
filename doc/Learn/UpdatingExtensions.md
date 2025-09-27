---
uid: Uno.Extensions.Migration
---

# Upgrading Extensions Version

## Upgrading to Extensions 7.0

### OidcClient Authentication

When upgrading to Uno.Extensions 7.0 or later, the NuGet Package Dependency, before known as `IdentityModel.OidcClient`, which is used in the [Oidc Authentication Extension](xref:Uno.Extensions.Authentication.HowToOidcAuthentication), has been [rebranded](https://github.com/DuendeSoftware/foss/blob/main/README.md#relationship-to-identitymodel).

When upgrading to later versions, you must make sure to update the Namespaces in your App, to match the new ones. e.g. in your `GlobalUsings.cs` file at your Project root directory:

```diff
- global using IdentityModel.OidcClient
+ global using Duende.IdentityModel.OidcClient
```

## Upgrading to Extensions 6.0

The Uno SDK dependencies for Uno Extensions have been updated to Uno SDK version 6.0. Ensure to [update your uno.sdk](xref:Uno.Development.UpgradeUnoNuget) to the latest version.

## Upgrading to Extensions 5.2

### MSAL Authentication

When upgrading to Uno.Extensions 5.2 or later, you must update your MSAL authentication setup in the host configuration. The `AddMsal()` method now includes an additional parameter to specify the `Window` instance used by the `MsalAuthenticationProvider` to configure the authentication dialog. You can obtain the `Window` instance from the `Configure()` method overload that provides it:

```diff
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
-       .Configure(host =>
+       .Configure((host, window) =>
        {
            host
            .UseAuthentication(builder =>
            {
-               builder.AddMsal();
+               builder.AddMsal(window);
            });
        });
    ...
}
```

> [!IMPORTANT]
> Failing to pass a valid `Window` instance could result in a `MsalClientException` with the message:
> *"Only loopback redirect uri is supported, but <your_redirect_uri> was found. Configure `http://localhost` or `http://localhost:port` both during app registration and when you create the PublicClientApplication object. See `https://aka.ms/msal-net-os-browser` for details."*
