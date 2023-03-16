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
                builder
                .AddWeb(web => 
                {
                    web
                    .PrefersEphemeralWebBrowserSession(/* Ephemeral session */)
                    .LoginStartUri(/* Login URI */)
                    .LoginCallbackUri(/* Login callback */)
                    .PostLogin(/* Post-login callback */)
                    .LogoutStartUri(/* Logout URI */)
                    .LogoutCallbackUri(/* Logout callback */)
                    .Refresh(/* Refresh callback */)
                    .AccessTokenKey(/* Access token key */)
                    .RefreshTokenKey(/* Refresh token key */)
                });
            });
        });
...
```

## Platform specific behavior

Before the `WebAuthenticationProvider` is automatically built, there are platform specific checks invoked internally which occasionally alter behavior during the authentication process:

**Windows**: The `AddWeb()` extension method will initialize a `WebAuthenticator` from WinUIEx for a better-integrated sign in flow. This is done preemtively to support its usage within `WebAuthenticationProvider` during login and logout instead of the `WebAuthenticationBroker` used for other platforms.

**iOS**: `WebAuthenticationBroker` will only respond to the `PrefersEphemeralWebBrowserSession` setting value in iOS 13+. Further, the other platforms will ignore this setting.