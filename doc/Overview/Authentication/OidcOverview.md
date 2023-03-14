---
uid: Overview.Authentication.Oidc
---

# Oidc

The `OidcAuthenticationProvider` allows users to sign in using their identities from a participating provider. It can wrap support for any [OpenID Connect](https://openid.net/connect/) backend, such as [IdentityServer](https://duendesoftware.com/products/identityserver) into an implementation of `IAuthenticationProvider`, and is included with the Uno.Extensions.Authentication.Oidc.WinUI or Uno.Extensions.Authentication.Oidc.UI NuGet packages.

## Obtain an OpenID Connect client ID

For this type of authentication, the application must already be registered with the desired identity provider. Through this process, a client id (and client secret) are obtained.

## Set up OpenID Connect authentication

To use OpenID Connect authentication, you need to provide the following information:

- Client id
- Client secret
- Scopes
- Authority
- Redirect Uri
- Post Logout Redirect Uri (optional)

The `OidcAuthenticationProvider` is added using the `AddOidc()` extension method which configures the `IAuthenticationBuilder` to use it. The following example shows how to configure the provider:

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
                .AddOidc(oidc => 
                {
                    oidc
                    .Scopes(/* Scopes */)
                    .ClientId(/* Client Id */)
                    .ClientSecret(/* Client Secret */)
                    .Authority(/* Authority */)
                    .RedirectUri(/* Redirect Uri */)
                    .PostLogoutRedirectUri(/* Post Logout Redirect Uri */)
                });
            });
        });
}
```

The `IAuthenticationBuilder` is responsible for managing the lifecycle of the associated provider that was built. Since it is configured to use Oidc, the user will be prompted to sign in to their identity provider when they launch the application. 

## Platform specific behavior

When the `OidcAuthenticationProvider` is automatically built, there are platform specific checks invoked internally which occasionally alter behavior during the authentication process:

**WebAssembly**: The `OidcAuthenticationProvider` will automatically use the `WebAuthenticationBroker` to obtain redirect URIs during the authentication process. This is done to avoid the need for a redirect to a custom URI scheme, which is not supported in the browser.
