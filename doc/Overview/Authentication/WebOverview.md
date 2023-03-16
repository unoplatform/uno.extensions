---
uid: Overview.Authentication.Web
---

# Web

The `WebAuthenticationProvider` provides an implementation that displays a web view in order for the user to login. After login, the web view redirects back to the application, along with any tokens. The `WebAuthenticationProvider` is included with the Uno.Extensions.Authentication.WinUI and Uno.Extensions.Authentication.UI NuGet packages.

## Set up web authentication

When using web authentication, you can provide the following information:

- Access token key
- Refresh token key
- Login URI
- Login callback
- Post-login callback
- Logout URI
- Logout callback
- Refresh callback
- Preference for ephemeral session

The `WebAuthenticationProvider` is added using the `AddWeb()` extension method which configures the `IAuthenticationBuilder` to use it. The following example shows how to configure the provider:

```csharp
private IHost Host { get; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host => 
        {
            host
            .UseAuthentication(builder => 
            {
                builder.AddWeb();
            });
        });
...
```

## Platform specific behavior

Before the `WebAuthenticationProvider` is automatically built, there are platform specific checks invoked internally which occasionally alter behavior during the authentication process:

**Windows**: The `AddWeb()` extension method will initialize a `WebAuthenticator` to launch an out-of-process browser. This is done preemtively to support its usage within `WebAuthenticationProvider` during login and logout instead of the `WebAuthenticationBroker` used for other platforms.

**Other platforms**: For a description of various subtle differences when displaying a web login prompt on multiple platforms, see [Web Authentication Broker](https://platform.uno/docs/articles/features/web-authentication-broker.html). The broker will only respond to the `PrefersEphemeralWebBrowserSession` setting value in iOS (versions 13.0+), while the other platforms will ignore it.