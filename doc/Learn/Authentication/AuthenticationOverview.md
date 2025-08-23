---
uid: Uno.Extensions.Authentication.Overview
---
# Authentication

> **UnoFeatures:** `Authentication`, `AuthenticationMsal`, or `AuthenticationOidc` (add to `<UnoFeatures>` in your `.csproj`)
>
> [!IMPORTANT]
>
> - Use `IAuthenticationService` from `Uno.Extensions.Authentication`
> - Inject `IAuthenticationService` into ViewModels via constructor parameters

Uno.Extensions.Authentication is designed to make it simple to add authentication to an application. Authenticating a user may be used to restrict access to specific parts of the application, or in order to supply an access token when calling a back-end service.

There are two aspects to the Authentication extensions:

- Authentication: the process of authenticating the user and acquiring tokens
- Authorization: tokens (acquired via authentication) can be queried to control access to parts of the application or supplied to service call so the user can be authorized to access the back-end service

## Installation

`Authentication` is provided as an Uno Feature. To enable `Authentication` support in your application, add `Authentication` to the `<UnoFeatures>` property in the Class Library (.csproj) file. In case you are using Msal Authentication add `AuthenticationMsal`, or `AuthenticationOidc` for Oidc Authentication.

[!include[existing-app](../includes/existing-app.md)]

[!include[single-project](../includes/single-project.md)]

For more information about `UnoFeatures` refer to our [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk) docs.

## IAuthenticationService

The `IAuthenticationService` is the framework-provided authentication interface that you inject into your ViewModels. It defines the methods that an application can call to authenticate the user.

```csharp
public interface IAuthenticationService
{
    string[] Providers { get; }
    ValueTask<bool> LoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials = default, string? provider = null, CancellationToken? cancellationToken = default);
    ValueTask<bool> RefreshAsync(CancellationToken? cancellationToken = default);
    ValueTask<bool> LogoutAsync(IDispatcher? dispatcher, CancellationToken? cancellationToken = default);
    ValueTask<bool> IsAuthenticated(CancellationToken? cancellationToken = default);
    event EventHandler LoggedOut;
}
```

There are any number of different application workflows that require authentication but they typically boil down to using one or more of the `IAuthenticationService` methods. For example:

### Login on launch

In this scenario the user is required to be authenticated in order to access the application. This is a workflow that redirects the user to a login prompt if they aren't authenticated when the application is launched.

- Launch app
- App invokes `IAuthenticationService.RefreshAsync` to refresh any existing tokens (eg retrieve a new Access Token by supplying a Refresh Token to an authentication endpoint).
- If `RefreshAsync` returns true, the user is logged in, so navigate to the home page of the application
- If `RefreshAsync` returns false, navigate to the login page of the application
- On the login page, user enter credentials and clicks Login, the app invokes `IAuthenticationService.LoginAsync` and supplies credentials
- If `LoginAsync` returns true, app navigates to home page
- The user might decide to logout of the application, which invokes the `IAuthenticationService.LogoutAsync` method, the application then navigates back to the login page.

### User login requested

In this scenario the application doesn't require the user to be authenticated unless they want to access certain parts of the application (or there is additional/different information that's available to the user if they've logged in)

- Launch app
- App invokes `IAuthenticationService.RefreshAsync` to refresh any existing tokens and to determine if the user is authenticated. The user is directed to the home page of the application, either as an unauthenticated or authenticated user (depending on the app, this may show different data).
- User attempts to navigate to a part of the application that needs them to be authenticated, or just clicks a sign-in button so they can access the current page as an authenticated user.
- App navigates to the login page where the user can enter their credentials. The app then invokes `IAuthenticationService.LoginAsync` to authenticate the user.
- If `LoginAsync` returns true, the user is then either navigated to the part of the application they were attempting to access, or back to the view they were on.
- The user can logout of the application, which again invokes the `IAuthenticationService.LogoutAsync` method

## Authentication Providers (IAuthenticationProvider)

The process of authentication with a given authority is implemented by an authentication provider (i.e. implements IAuthenticationProvider). Multiple providers, and in fact, multiple instances of any provider, can be registered during application startup. For example, an application may want to provide support for authenticating using Facebook, Twitter and Apple - each of these has a different backend service that needs to be connected with. In the application the user selects which registered provider to use by supplying the `provider` argument when invoking the `IAuthenticationService.LoginAsync` method. This argument is optional and is not required if only a single provider has been registered.

> [!NOTE]
> The `IAuthenticationProvider` implementations are all marked as internal as they should be configured via the extensions methods on the `IHostBuilder` and the builder interface for the corresponding implementation.

### [Custom](#tab/custom)

The `CustomAuthenticationProvider` provides a basic implementation of the `IAuthenticationProvider` that requires callback methods to be defined for performing login, refresh and logout actions. Learn [Custom authentication](xref:Uno.Extensions.Authentication.HowToAuthentication)

### [MSAL](#tab/msal)

The `MsalAuthenticationProvider` wraps the [MSAL library](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) from Microsoft into an implementation of `IAuthenticationProvider`. This implementation ignores any credentials passed into the `LoginAsync` method, instead invoking the web based authentication process required to authentication with Microsoft. Learn [Msal authentication](xref:Uno.Extensions.Authentication.HowToMsalAuthentication)

Find MSAL Authentication also implemented in the [Uno ToDo App Sample](https://github.com/unoplatform/Uno.Samples/blob/master/reference/ToDo/src/ToDo/Business/AuthenticationService.cs).

**The following Lookups are from Microsoft Entra about the Authentication process using MSAL**:

- [How to use Continuous Access Evaluation enabled APIs in your applications](https://learn.microsoft.com/en-us/entra/identity-platform/app-resilience-continuous-access-evaluation?tabs=dotnet)
- [Scenario Desktop app configuration](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-desktop-app-configuration?tabs=dotnet)
- [Scenario Desktop acquire tokens interactively](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-desktop-acquire-token-interactive?tabs=dotnet)

### [Oidc](#tab/oidc)

The `OidcAuthenticationProvider` wraps support for any [OpenID Connect](https://openid.net/connect/) backend, including [IdentityServer](https://duendesoftware.com/products/identityserver). Learn [Oidc authentication](xref:Uno.Extensions.Authentication.HowToOidcAuthentication)

**Microsoft Entra OIDC Documentation**:

- [Oidc Protocol](https://learn.microsoft.com/en-us/entra/identity-platform/v2-protocols-oidc)
- [UserInfo Endpoint](https://learn.microsoft.com/en-us/entra/identity-platform/userinfo)

**Alternative Oidc Libraries**:

- [OpenIdDict for OIDC Authentication](https://documentation.openiddict.com/introduction)
  - [Integration with Operating Systems](https://documentation.openiddict.com/integrations/operating-systems)
  - [Integration with System.Net.Http](https://documentation.openiddict.com/integrations/system-net-http)
  - [Web Providers](https://documentation.openiddict.com/integrations/web-providers)
  - [Integrating with Entity Framework Core](https://documentation.openiddict.com/integrations/entity-framework-core)

#### [Platform-specific behavior](#tab/oidc/platform-specifics)

When the `OidcAuthenticationProvider` is automatically built, there are platform specific checks invoked internally which occasionally alter behavior during the authentication process:

**WebAssembly**: The `OidcAuthenticationProvider` will automatically use the `WebAuthenticationBroker` to obtain redirect URIs during the authentication process. This is done to avoid the need for a redirect to a custom URI scheme, which is not supported in the browser.

### [Web](#tab/web)

The `WebAuthenticationProvider` provides an implementation that displays a web view in order for the user to login. After login, the web view redirects back to the application, along with any tokens. Learn [Web Authentication](xref:Uno.Extensions.Authentication.HowToWebAuthentication)

#### [Platform-specific behavior](#tab/web/platform-specifics)

Before the `WebAuthenticationProvider` is automatically built, there are platform specific checks invoked internally which occasionally alter behavior during the authentication process:

**Windows**: The `AddWeb()` extension method will initialize a `WebAuthenticator` to launch an out-of-process browser. This is done preemptively to support its usage within `WebAuthenticationProvider` during login and logout instead of the `WebAuthenticationBroker` used for other platforms.

You can find an alternative for Device Protocol usage on Windows in the [oAuth2Manager coming from then WinAppSdk](https://learn.microsoft.com/de-de/windows/apps/develop/security/oauth2?tabs=csharp) and [its Sample Project](https://github.com/microsoft/WindowsAppSDK-Samples/blob/release/experimental/Samples/OAuth2Manager/README.md). For using this, make sure you lookup the [Uno Specific Docs for Protocol Activation](https://platform.uno/docs/articles/features/protocol-activation.html#handling-protocol-activation) and [Windowing API](https://platform.uno/docs/articles/features/windows-ui-xaml-window.html#explaining-basic-windowing-apis) because there are slightly differences you should acknowledge to avoid problems.

**Other platforms**: For a description of various subtle differences when displaying a web login prompt on multiple platforms, see [Web Authentication Broker](https://platform.uno/docs/articles/features/web-authentication-broker.html). The broker will only respond to the `PrefersEphemeralWebBrowserSession` setting value in iOS (versions 13.0+), while the other platforms will ignore it.

### [WebConfiguration Options](#tab/web/config-options)

The `WebAuthenticationProvider` can be configured using the `WebConfiguration` defined properties.

The following properties are available:

```csharp
internal record WebConfiguration
{
	public bool PrefersEphemeralWebBrowserSession { get; init; }
	public string? LoginStartUri { get; init; }
	public string? LoginCallbackUri { get; init; }
	public string? AccessTokenKey { get; init; }
	public string? RefreshTokenKey { get; init; }
	public string? IdTokenKey { get; init; }
	public IDictionary<string, string>? OtherTokenKeys { get; init; }
	public string? LogoutStartUri { get; init; }
	public string? LogoutCallbackUri { get; init; }
}
```

So for example, in your `appsettings.json` file, you could include the following configuration:

```json
{
	"Web": {
		"LoginStartUri": "https://example.com/login",
        "LoginCallbackUri": "https://example.com/signin-provider-callback",
        "IdTokenKey": "id_token",
		"LogoutStartUri": "https://example.com/logout",
        "LogoutCallbackUri": "https://example.com/logout-provider-callback",
        "AccessTokenKey": "access_token",
        "RefreshTokenKey": "refresh_token",
        "OtherTokenKeys": {
            "custom_token_key": "custom_token"
        }
	}
}
```

The Token keys are assumed to be the response keys returned from the identity provider, that will be used in the response url infront of the token value. For example, the `access_token` key is used to retrieve the access token from the response like this:

```
access_token=fsjaiafjioangosafn&expires_in=3600&token_type=Bearer
```

The `LoginCallbackUri` and `LogoutCallbackUri` are used to redirect the user back to the application after they have logged in or logged out. These URIs should be registered with the identity provider.

You can use the `PostLogin` Callback to perform any additional processing after the user has logged in, such as retrieving user information from a user info endpoint, or extracting additional tokens from the response url, which is provided as string argument in the `AsyncFunc`. To make the Authentication Process fail e.g. if you discovered the Response contains an Error response, you can simply make the `PostLogin` return `null` or `default`.

> [!NOTE]
> The `AccessTokenKey`, `RefreshTokenKey`, and `IdTokenKey` are used to store the tokens in the credential storage after the `PostLogin` has been returned. The `OtherTokenKeys` dictionary can be used to store any additional tokens that are returned by the identity provider, but are not read from the WebAuthenticationProvider of Uno at this time.
> [!NOTE]
> Usually, the `AccessToken` and `RefreshToken` will be returned from the `Token` endpoint, which is needed to be called in the `PostLogin` Callback you can register for in the Web Authentication registration in your App.xaml.cs, which is called after the user has authenticated to your identity server instance or authorized your application to [act on-behalf-of the user](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow) to an external API. *(This Link is specific to Microsoft Entra ID, but the concept applies to other identity providers as well.)*

---

## Http Handlers

Once a user has been authenticated, the tokens are cached and are available for use when invoking service calls. Rather than developers having to access the tokens and manually appending the tokens to the http request, the Authentication extensions includes http handlers which will be inserted into the request pipeline in order to apply the tokens as required.

- [Learn more about Http Extensions in your Uno Apps](xref:Uno.Extensions.Http.Overview)

### Authorization Header

The `HeaderHandler` is used to apply the access token to the http request using the `Authorization` header. The default scheme is `Bearer` but this can be override to use a different scheme, such as basic.

- [Using Http Header Properties](https://learn.microsoft.com/de-de/dotnet/fundamentals/networking/http/httpclient-migrate-from-httpwebrequest#usage-of-header-properties)

### Cookies

The `CookieHandler` is used to apply the access token, and/or refresh token, to the http request as cookies. This requires the cookie name for access token and request token to be specified as part of configuring the application. Learn how to use [Cookies](xref:Uno.Extensions.Authentication.HowToCookieAuthorization) to authorize.
The `CookieHandler` is used to apply the access token, and/or refresh token, to the http request as cookies. This requires the cookie name for access token and request token to be specified as part of configuring the application. Learn how to use [Cookies](xref:Uno.Extensions.Authentication.HowToCookieAuthorization) to authorize

## Future Readings

### Microsoft

The following links should give you a first overview over most of the oAuth2 (integrated in the Web Authentication e.g. on Windows) and OpenID Connect Authorization concepts with visual diagrams and links to Microsoft specific sample apps e.g. useful if you choose MSAL Authentication:

- [Authorization Code Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow)
- [Client Credentials Grant Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-client-creds-grant-flow)
- [Device Code Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-device-code)
- [On-Behalf-Of Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow)
- [Implicit Grant Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-implicit-grant-flow)
- [Resource Owner Password Credentials Grant](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth-ropc)
- [Redirect or Reply URLs](https://learn.microsoft.com/en-us/entra/identity-platform/reply-url)
- [Use a state parameter to prevent cross-site request forgery attacks](https://learn.microsoft.com/en-us/entra/identity-platform/reply-url#use-a-state-parameter)

### Additional Resources

- [Securing secrets in .NET 8 with Azure Key Vault from 'The Code Man'](https://thecodeman.net/posts/securing-secrets-in-dotnet-with-azure-key-vault)
- [Using HttpClient and HttpRequestMessage Properties for Authentication Redirect and Callback actions](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-migrate-from-httpwebrequest#usage-of-httpclient-and-httprequestmessage-properties)
- [The System.Net.Http.HttpClient Class](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-net-http-httpclient)
- [The System.Net.Http.HttpListener Class to handle callbacks](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-net-httplistener)
- [Using TLS and SSL for Secure Web Communication using Certificates](https://learn.microsoft.com/en-us/dotnet/core/extensions/sslstream-best-practices)
- [Breaking Changes for Default TLS cipher suites in .NET on Linux](https://learn.microsoft.com/en-us/dotnet/core/compatibility/cryptography/5.0/default-cipher-suites-for-tls-on-linux?source=recommendations)
- [API `System.Security.Cryptography.Pkcs` removed from .NET `9.0.3`](https://learn.microsoft.com/en-us/dotnet/core/compatibility/cryptography/9.0/api-removed-pkcs)
- [Use the System Browser to open the Authentication UI for a interactive Authentication Flow](https://learn.microsoft.com/de-de/windows/apps/develop/launch/launch-default-app)
- [WebView2 for BasicAuthentication interaction](https://learn.microsoft.com/de-de/microsoft-edge/webview2/concepts/basic-authentication?tabs=csharp) - [Uno specifics](https://platform.uno/docs/articles/controls/WebView.html)

### Server Project specific Information and Resources
<!-- cspell: ignore HSTS Antiforgery -->
- [Using Authentication and Authorization in your Server Minimal API ASP NET Core Project](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-9.0)
- [Protect Secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-9.0&tabs=windows)
- [Enforce HTTPS](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-9.0&tabs=visual-studio%2Clinux-sles) <!-- TODO: Add information about HSTS usage in Uno Wasm and Uno.Wasm.Bootstrapper.Server using Server Project -->
- [Antiforgery in Minimal API against Cross-Site Request Forgery (CSRF/XSRF)](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-9.0#antiforgery-with-minimal-apis) e.g. this is the reason for oAuth PKCE Token Challenging!
- [Allowing Requests like Authentication from same Origin with Cors Policy](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-9.0)
- [Share Cookies Among your Apps](https://learn.microsoft.com/en-us/aspnet/core/security/cookie-sharing?view=aspnetcore-9.0) or [Using SameSite Cookies](https://learn.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-9.0)
- [Learn path about how to use a Entity Framework Core Database in a Minimal API Project](https://learn.microsoft.com/en-us/training/modules/build-web-api-minimal-database/?view=aspnetcore-9.0)
- [Map Static files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/map-static-files?view=aspnetcore-9.0)
- [Use Static file authorization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-9.0#static-file-authorization)
