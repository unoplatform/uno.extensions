---
uid: Uno.Extensions.Authentication.CookieAuthorization
title: Persist Tokens in Cookies
tags: [authentication, cookies, tokens]
---
# Reuse authentication tokens stored in cookies

Capture access and refresh tokens from HTTP responses, store them in cookies, and replay them on future authentication attempts.

> [!IMPORTANT]
> Set up a baseline authentication flow first, such as the one in [Authenticate Users with Custom Logic](xref:Uno.Extensions.Authentication.HowToAuthentication). Cookie support layers on top of any `IAuthenticationProvider`.

## Opt in to cookie-based storage

Register `UseAuthentication` and add the cookie handler to the authorization pipeline.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseAuthentication(
                auth => auth.AddCustom(custom =>
                    custom.Login(async (sp, dispatcher, cache, credentials, ct) =>
                    {
                        var isValid = credentials.TryGetValue("Username", out var username)
                                      && username == "Bob";
                        return isValid ? credentials : default!;
                    })),
                configureAuthorization: builder => builder.Cookies());
        });
}
```

`Cookies()` switches the HTTP message handler so responses are scanned for token values and written into platform-specific cookie stores.

## Name the cookie values

Provide cookie names that distinguish your access and refresh tokens.

```csharp
host.UseAuthentication(
    auth => auth.AddCustom(...),
    configureAuthorization: builder =>
        builder.Cookies(accessTokenName: "AccessToken", refreshTokenName: "RefreshToken"));
```

The refresh token name is optional—omit it if your provider only issues a single token.

## Authenticate using stored cookies

After enabling cookie handling, `LoginAsync`, `RefreshAsync`, and other authentication calls attempt to load the access token from the named cookie before invoking the provider. When a cookie is present, the request immediately succeeds and the token is added to outbound HTTP headers. If the cookie is missing or expired, the provider flow runs as normal and writes updated tokens back to the cookie once the response arrives.

For guidance on invoking the authentication service from a view model, follow [Authenticate Users with Custom Logic](xref:Uno.Extensions.Authentication.HowToAuthentication).

## Resources

- [Authentication overview](xref:Uno.Extensions.Authentication.Overview)
- [Authenticate users with custom logic](xref:Uno.Extensions.Authentication.HowToAuthentication)
- [What is a cookie?](https://developer.mozilla.org/en-US/docs/Web/HTTP/Cookies)
- [Access tokens](https://oauth.net/2/access-tokens/)
- [Refresh tokens](https://oauth.net/2/refresh-tokens/)
