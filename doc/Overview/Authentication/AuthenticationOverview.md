---
uid: Overview.Authentication
---
# Authentication

The Authentication extensions are designed to make it simple to add authentication to an application. This may be to restrict access to specific parts of the application, or in order to supply an access token when calling a back-end service. 

There are three aspects to the Authentication extensions:
- Authentication - the the process of authenticating the user and acquiring tokens
- Authorization - tokens (acquired via authentication) can be queried to control access to parts of the application or supplied to service call so the user can be authorized to access the back-end service
- Authentication Flow - this is the user experience of authenticating the user, which typically involves navigating to a login screen and subsequently to a page within the application that requires the user to be authenticated.

## IAuthenticationService

The `IAuthenticationService` interface defines the methods that an application can call to authenticate the user.  

````csharp
public interface IAuthenticationService
{
	Task<bool> CanRefresh();
	Task<bool> LoginAsync(IDispatcher dispatcher, CancellationToken cancellationToken);
	Task<bool> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken);
	Task<bool> RefreshAsync(CancellationToken cancellationToken);
	Task<bool> LogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken);
}
````
There are any number of different application workflows that require authentication but they typically boil down to using one or more of the `IAuthenticationService` methods. For example

**Login on launch**
In this scenario the user is required to be authenticated in order to access the application. This is a simple workflow that simply redirects the user to a login prompt if they aren't authenticated.
-   Launch app
-   App invokes `RefreshAsync` to determine if the user is authenticated. 
-   If `RefreshAsync` returns true, the user is logged in, so navigate to the home page of the application
-   If `RefreshAsync` returns false, navigate to the login page of the application
-   User enter credentials and clicks Login, the app invokes LoginAsync and supplies credentials)
-   If `LoginAsync` returns true, app navigates to home page
-   The user might decide to logout of the application, which invokes the `LogoutAsync` method, the application then navigates back to the login page.

**User login requested**
In this scenario the application doesn't require the user to be authenticated unless they want to access certain parts of the application (or there is additional/different information that's available to the user if they've logged in)
-   Launch app
-   App invokes `RefreshAsync` to determine if the user is authenticated. The user is directed to the home page of the application, either as an unauthenticated or authenticated user (depending on the app, this may show different data).
-   User attempts to navigate to a part of the application that needs them to be authenticated, or just clicks a sign-in button so they can access the current page as an authenticated user.
-   App navigates to the login page where the user can enter their credentials. The app then invokes `LoginAsync` to authenticate the user.
-   If `LoginAsync` returns true, the user is then either navigated to the part of the application they were attempting to access, or back to the view they were on.
-   The user can logout of the application, which again invokes the `LogoutAsync` method

### Custom
The `CustomAuthenticationService` provides a basic implementation of the `IAuthenticationService` that requires callback methods to be defined for performing login, refresh and logout actions. 

### MSAL
The `MsalAuthenticationService` wraps the `MSAL` library from Microsoft into an implementation of `IAuthenticationService`. This implementation ignores any credentials passed into the `LoginAsync` method, instead invoking the web based authentication process required to authentication with Microsoft.

## Http Handlers
Once a user has been authenticated, the tokens are cached and are available for use when invoking service calls. Rather than developers having to access the tokens and manually appending the tokens to the http request, the Authentication extensions includes http handlers which will be inserted into the request pipeline in order to apply the tokens as required.

### Authorization Header
The `HeaderHandler` (TODO: Currently this is called HeaderAuthorizationHandler but I think either AuthorizationHeaderHandler or just HeaderHandler would be better) is used to apply the access token to the http request using the `Authorization` header. The default scheme is `Bearer` but this can be override to use a different scheme, such as basic.

### Cookies
(TODO: Rename CookieAuthenticationHandler to just CookieHandler)
The `CookieHandler` is used to apply the access token, and/or refresh token, to the http request as cookies. This requires the cookie name for access tokena and request token to be specified as part of configuring the application.

## IAuthenticationFlow
The `IAuthenticationFlow` interface is designed to reduce the need for applications to define authentication workflow code. 
```csharp
public interface IAuthenticationFlow
{
	void Initialize(IDispatcher dispatcher, INavigator navigator);

	Task<NavigationResponse?> AuthenticatedNavigateAsync(NavigationRequest request, INavigator? navigator = default, CancellationToken ct = default);
	Task<bool> EnsureAuthenticatedAsync(CancellationToken ct = default);
	Task<bool> LoginAsync(IDictionary<string, string>? credentials, CancellationToken ct = default);
	Task<bool> LogoutAsync(CancellationToken ct = default);
}
```

The built-in `AuthenticationFlow` has been designed to cater for a variety of authentication workflows. The following examples describe use of `IAuthenticationFlow` to cover the earlier scenarios.  

**Login on launch**
-   Launch app
-   App invokes `AuthenticatedNavigateAsync` navigate to the home page of the application once the user has been successfully authenticated. 
-   The user might decide to logout of the application, which invokes the `LogoutAsync` method, which will navigate back to the login page.

**User login requested**
-   Launch app
-   App invokes `RefreshAsync` to determine if the user is authenticated. The user is directed to the home page of the application, either as an unauthenticated or authenticated user (depending on the app, this may show different data).
-   User attempts to navigate to a part of the application that needs them to be authenticated, or just clicks a sign-in button so they can access the current page as an authenticated user. The app invokes `AuthenticatedNavigateAsync` to authenticate the user and then navigates the user to the requested page in the app.
-   The user can logout of the application, which invokes the `LogoutAsync` method, keeping the user on the current page.


