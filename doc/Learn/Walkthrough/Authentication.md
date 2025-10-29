---
uid: Uno.Extensions.Authentication.Authentication
title: Authenticate Users with Custom Logic
tags: [authentication, custom-login, navigation]
---
# Onboard users with custom credential checks

Add Uno authentication to your app, validate credentials in a custom callback, and route users to the right view.

## Enable the authentication feature

Add the `Authentication` feature to bring in `Uno.Extensions.Authentication`.

```diff
<UnoFeatures>
    Material;
+   Authentication;
    Toolkit;
    MVUX;
</UnoFeatures>
```

> [!TIP]
> On iOS and Mac Catalyst, the authentication extension stores secrets in the Apple Keychain. Provision the [required entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements) so the app can persist tokens.

## Validate credentials in the host

Use `AddCustom` to handle authentication inside the host configuration.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseAuthentication(auth =>
                auth.AddCustom(custom =>
                    custom.Login(async (sp, dispatcher, tokenCache, credentials, ct) =>
                    {
                        var isValid = credentials.TryGetValue("Username", out var username) && username == "Bob";
                        return isValid ? credentials : default!;
                    })));
        });
}
```

Return a dictionary with any tokens or claims when validation succeeds; return `default` to deny the login.

## Trigger login from the view model

Bind your view to inputs and call `LoginAsync` from the view model.

```xml
<StackPanel>
    <TextBox Text="{Binding Username, Mode=TwoWay}" />
    <Button Content="Login"
            Click="{x:Bind ViewModel.Authenticate}" />
</StackPanel>
```

```csharp
public class MainViewModel
{
    private readonly IAuthenticationService _auth;
    private readonly IDispatcher _dispatcher;
    private readonly INavigator _navigator;

    public string? Username { get; set; }

    public MainViewModel(IDispatcher dispatcher, INavigator navigator, IAuthenticationService auth)
    {
        _dispatcher = dispatcher;
        _navigator = navigator;
        _auth = auth;
    }

    public async Task Authenticate()
    {
        var credentials = new Dictionary<string, string>
        {
            ["Username"] = Username ?? string.Empty
        };

        if (await _auth.LoginAsync(_dispatcher, credentials, CancellationToken.None))
        {
            await _navigator.NavigateViewModelAsync<SecondViewModel>(this);
        }
    }
}
```

`LoginAsync` caches successful credentials, so restarting the app keeps the user signed in until refresh fails.

## Route authenticated users automatically

Check `RefreshAsync` on startup to send users to the correct view.

```csharp
public class ShellViewModel
{
    private readonly IAuthenticationService _auth;
    public INavigator Navigator { get; }

    public ShellViewModel(IAuthenticationService auth, INavigator navigator)
    {
        _auth = auth;
        Navigator = navigator;
    }

    public async Task Start()
    {
        if (await _auth.RefreshAsync(CancellationToken.None))
        {
            await Navigator.NavigateViewModelAsync<SecondViewModel>(this);
        }
        else
        {
            await Navigator.NavigateViewModelAsync<MainViewModel>(this);
        }
    }
}
```

Add route dependencies so navigation history stays intact.

```csharp
routes.Register(
    new RouteMap("",
        View: views.FindByViewModel<ShellViewModel>(),
        Nested: new[]
        {
            new RouteMap("Main", View: views.FindByViewModel<MainViewModel>()),
            new RouteMap("Second", View: views.FindByViewModel<SecondViewModel>(), DependsOn: "Main"),
        }));
```

Provide a logout action to clear tokens.

```xml
<Button Content="Logout"
        Click="{x:Bind ViewModel.Logout}" />
```

```csharp
public record SecondViewModel(IDispatcher Dispatcher, IAuthenticationService Auth)
{
    public async Task Logout() =>
        await Auth.LogoutAsync(Dispatcher, CancellationToken.None);
}
```

# Delegate sign-in to an HTTP endpoint

Call a backend service during login and store the issued token.

## Enable HTTP client support

Add the `Http` feature so you can register Refit clients.

```diff
<UnoFeatures>
    Material;
    Authentication;
+   Http;
    Toolkit;
    MVUX;
</UnoFeatures>
```

Define the DTOs and Refit interface for the remote login endpoint.

```csharp
using Refit;

[Headers("Content-Type: application/json")]
public interface IDummyJsonEndpoint
{
    [Post("/auth/login")]
    Task<AuthResponse> LoginAsync(Credentials credentials, CancellationToken ct);
}

public sealed class Credentials
{
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("password")]
    public string? Password { get; init; }
}

public sealed class AuthResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; init; }
}
```

## Register the Refit client and authentication flow

Configure both the HTTP client and custom authentication.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseHttp((context, services) =>
            {
                services.AddRefitClient<IDummyJsonEndpoint>(context);
            });

            host.UseAuthentication(auth =>
                auth.AddCustom<IDummyJsonEndpoint>(custom =>
                    custom.Login(async (api, dispatcher, tokenCache, credentials, ct) =>
                    {
                        var username = credentials.GetValueOrDefault("Username");
                        var password = credentials.GetValueOrDefault("Password");

                        var response = await api.LoginAsync(
                            new Credentials { Username = username, Password = password },
                            ct);

                        if (!string.IsNullOrEmpty(response?.Token))
                        {
                            credentials["AccessToken"] = response.Token;
                            return credentials;
                        }

                        return default!;
                    })));
        });
}
```

> [!NOTE]
> The generic `AddCustom<TService>` overload injects the registered `IDummyJsonEndpoint` into the login callback instead of the raw service provider.

## Configure the endpoint

Set the base URL in `appsettings.json` to map the Refit client.

```json
{
  "DummyJsonEndpoint": {
    "Url": "https://dummyjson.com",
    "UseNativeHandler": true
  }
}
```

The section name must match the interface without the leading `I`.

## Collect credentials and call the API

Capture both username and password before invoking `LoginAsync`.

```xml
<StackPanel>
    <TextBox Text="{Binding Username, Mode=TwoWay}" />
    <TextBox Text="{Binding Password, Mode=TwoWay}" />
    <Button Content="Login"
            Click="{x:Bind ViewModel.Authenticate}" />
</StackPanel>
```

```csharp
public string? Password { get; set; }

public async Task Authenticate()
{
    var credentials = new Dictionary<string, string>
    {
        [nameof(Username)] = Username ?? string.Empty,
        [nameof(Password)] = Password ?? string.Empty
    };

    if (await _auth.LoginAsync(_dispatcher, credentials, CancellationToken.None))
    {
        await _navigator.NavigateViewModelAsync<SecondViewModel>(this);
    }
}
```

On success the token returned by the backend is cached alongside the credentials, so the next refresh can reuse it.

## Resources

- [Register HTTP endpoints](xref:Uno.Extensions.Http.HowToHttp)
- [Configure custom endpoint options](xref:Uno.Extensions.Http.HowToEndpointOptions)
- [Authentication overview](xref:Uno.Extensions.Authentication.Overview)
- [Navigation overview](xref:Uno.Extensions.Navigation.Overview)
