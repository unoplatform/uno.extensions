---
uid: Uno.Extensions.Migration
---

# Upgrading Extensions Version

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

Failing to pass a valid `Window` instance could result in a `MsalClientException` with the message:
*"Only loopback redirect uri is supported, but <your_redirect_uri> was found. Configure http://localhost or http://localhost:port both during app registration and when you create the PublicClientApplication object. See https://aka.ms/msal-net-os-browser for details."*
