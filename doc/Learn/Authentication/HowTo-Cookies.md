---
uid: Uno.Extensions.Authentication.HowToCookieAuthorization
---
# How-To: Using Cookies to Authorize

Using **cookies** is a common way to store tokens that are needed to authenticate a user. When an HTTP request is successfully authenticated, the server will return a response that creates a cookie containing a token value. Uno Extensions makes these cookie-related authorization steps less tedious by doing the work of extracting these values and applying them to future requests. This tutorial will teach you how to configure authentication to apply tokens from a cookie when they are available.

> [!IMPORTANT]
> To follow these steps, you first need to have an authentication system set up. We recommend choosing one of the `IAuthenticationProvider` implementations provided by Uno Extensions. Cookie authorization can complement any of the tutorials such as [Get Started with Authentication](xref:Uno.Extensions.Authentication.HowToAuthentication).

## Step-by-step

### 1. Enable cookies

- The first step is to opt-in to using cookies. This will allow for writing of returned access and refresh tokens to a cookie, and enables future reading of tokens from the cookie when they are available. You will also be able to name the tokens. Your app should already have an `IHostBuilder` configured to use an authentication provider like below:

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host =>
            {
                host.UseAuthentication(auth =>
                    auth.AddCustom(custom =>
                        custom.Login(
                            async (sp, dispatcher, tokenCache, credentials, cancellationToken) =>
                            {
                                var isValid = credentials.TryGetValue("Username", out var username) && username == "Bob";
                                return isValid ?
                                credentials : default;
                            })
                ));
            });
        ...
    }
    ```

- Modify the app to add the `Cookies()` extension method. Since the default HTTP request handler used does _not_ read tokens from cookies, this method will configure the `IAuthenticationBuilder` by registering a special handler that will parse the response for tokens and store them in a cookie. It will apply them to future requests.

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host =>
            {
                host.UseAuthentication(auth =>
                    auth.AddCustom(custom =>
                        custom.Login(
                            async (sp, dispatcher, tokenCache, credentials, cancellationToken) =>
                            {
                                var isValid = credentials.TryGetValue("Username", out var username) && username == "Bob";
                                return isValid ?
                                credentials : default;
                            })
                ),
                configureAuthorization: builder =>
                {
                    builder
                        .Cookies(/* options */);
                });
            });
        ...
    }
    ```

### 2. Configure cookie options

- The `Cookies()` extension method takes two parameters; the first represents a name for the [access token](https://oauth.net/2/access-tokens/) cookie, and the second represents a name for the [refresh token](https://oauth.net/2/refresh-tokens/) cookie.

    ```csharp
    configureAuthorization: builder =>
    {
        builder
            .Cookies("AccessToken", "RefreshToken");
        ...
    }
    ```

- Specifying a value for the latter is optional.

### 3. Authorize with a token value from a cookie

- With the appropriate handler enabled using the `Cookies()` extension method, attempts to authenticate with a provider will now try to authorize from a cookie. Access and refresh token information will be included in subsequent requests. If the cookie is not found, it will instead authenticate with the provider as normal.

- For more information on how to call the authentication service from a view model, see [Get Started with Authentication](xref:Uno.Extensions.Authentication.HowToAuthentication).

## See also

- [Authentication](xref:Uno.Extensions.Authentication.Overview)
- [Get Started with Authentication](xref:Uno.Extensions.Authentication.HowToAuthentication)
- [What is a cookie?](https://developer.mozilla.org/en-US/docs/Web/HTTP/Cookies)
- [Access tokens](https://oauth.net/2/access-tokens/)
- [Refresh tokens](https://oauth.net/2/refresh-tokens/)
