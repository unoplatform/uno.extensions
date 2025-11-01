---
uid: Uno.Extensions.Authentication.HowTo
title: Authenticate Users with Custom Logic
tags: [authentication, custom-login, navigation]
---

Here’s a RAG-friendly, outcome-first rewrite of that page, split into small, single-purpose how-tos.

Each how-to:

* starts with the **outcome** (not the API name),
* has minimal prerequisites,
* shows the code right away,
* doesn’t branch (“if you want X, else Y…” → separate how-tos instead),
* and repeats bits on purpose so each chunk can stand alone.

Source for all of this: ([Uno Platform][1])

---

## 1. Sign in with a hard-coded user

**Goal**
Accept a username, check it yourself (no server), and mark the user as authenticated.

**Dependencies**

* `Uno.Extensions.Authentication`
* `Uno.Extensions.Hosting`
* `Uno.Extensions.Navigation` (to navigate after login)

**1. Enable Authentication feature in the shared .csproj**

```xml
<PropertyGroup>
  <UnoFeatures>
    Material;
    Authentication;
    Toolkit;
    MVUX;
  </UnoFeatures>
</PropertyGroup>
```

This turns on the authentication extension. ([Uno Platform][1])

**2. Register authentication in `App.xaml.cs` (or the app host)**

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host
            .UseAuthentication(auth =>
                auth.AddCustom(custom =>
                    custom.Login(async (sp, dispatcher, tokenCache, credentials, ct) =>
                    {
                        var isValid =
                            credentials.TryGetValue("Username", out var username)
                            && username == "Bob";

                        return isValid ? credentials : default;
                    })
                ));
        });

    Host = builder.Build();
}
```

What happens:

* we add a **custom** auth provider,
* we get a credential dictionary (like `{ "Username": "Bob" }`),
* we return **the same dictionary** to signal success,
* or `default` to signal failure. ([Uno Platform][1])

**3. Bind the login UI to viewmodel**

```xml
<!-- MainPage.xaml -->
<TextBox Text="{Binding Username, Mode=TwoWay}" />
<Button Content="Login"
        Click="{x:Bind ViewModel.Authenticate}" />
```

Simple: user types, button calls `Authenticate()`. ([Uno Platform][1])

**4. Call the authentication service from the viewmodel**

```csharp
public class MainViewModel
{
    public string? Username { get; set; }

    private readonly IAuthenticationService _auth;
    private readonly IDispatcher _dispatcher;
    private readonly INavigator _navigator;

    public MainViewModel(
        IDispatcher dispatcher,
        INavigator navigator,
        IAuthenticationService auth)
    {
        _dispatcher = dispatcher;
        _navigator = navigator;
        _auth = auth;
    }

    public async Task Authenticate()
    {
        var success = await _auth.LoginAsync(
            _dispatcher,
            new Dictionary<string, string>
            {
                { "Username", Username ?? string.Empty }
            },
            CancellationToken.None);

        if (success)
        {
            await _navigator.NavigateViewModelAsync<SecondViewModel>(this);
        }
    }
}
```

Notes:

* `LoginAsync` needs a dispatcher → inject it.
* On success we navigate. ([Uno Platform][1])

---

## 2. Start the app already signed in (if token exists)

**Goal**
When the app starts, check if we have stored credentials; if yes, go straight to the “signed-in” view.

**Dependencies**

* `Uno.Extensions.Authentication`
* `Uno.Extensions.Navigation`

**1. In your shell/root viewmodel, refresh at startup**

```csharp
public class ShellViewModel
{
    private readonly IAuthenticationService _auth;
    public INavigator Navigator { get; }

    public ShellViewModel(
        INavigator navigator,
        IAuthenticationService auth)
    {
        Navigator = navigator;
        _auth = auth;
    }

    public async Task Start()
    {
        if (await _auth.RefreshAsync(CancellationToken.None))
        {
            // already authenticated → go to Second
            await Navigator.NavigateViewModelAsync<SecondViewModel>(this);
        }
        else
        {
            // not authenticated → go to Main (login)
            await Navigator.NavigateViewModelAsync<MainViewModel>(this);
        }
    }
}
```

`RefreshAsync` is the single “am I still signed in?” call. If it returns `true`, we skip login. ([Uno Platform][1])

**2. Make sure routes are registered with a shell**

```csharp
routes
    .Register(
        new RouteMap("",
            View: views.FindByViewModel<ShellViewModel>(),
            Nested: new[]
            {
                new RouteMap("Main",   View: views.FindByViewModel<MainViewModel>()),
                new RouteMap("Second", View: views.FindByViewModel<SecondViewModel>(), DependsOn: "Main"),
            }
        ));
```

`DependsOn: "Main"` ensures a proper back stack even if we jump to the “Second” view. ([Uno Platform][1])

---

## 3. Navigate after login

**Goal**
After a successful login, go to a page/viewmodel that needs an authenticated user.

**Dependencies**

* `Uno.Extensions.Navigation`
* Auth already configured (see How-to 1)

**Viewmodel**

```csharp
public async Task Authenticate()
{
    var ok = await _auth.LoginAsync(
        _dispatcher,
        new Dictionary<string, string>
        {
            { "Username", Username ?? string.Empty }
        },
        CancellationToken.None);

    if (ok)
    {
        await _navigator.NavigateViewModelAsync<SecondViewModel>(this);
    }
}
```

This is the minimal “login → go somewhere” pattern. ([Uno Platform][1])

---

## 4. Sign out from a protected page

**Goal**
Show a **Logout** button on a page that’s only useful when the user is signed in.

**Dependencies**

* `Uno.Extensions.Authentication`
* `Uno.Extensions.Navigation` (optional, if you want to navigate after logout)

**1. Add a Logout button to the page**

```xml
<!-- SecondPage.xaml -->
<Button Content="Logout"
        Click="{x:Bind ViewModel.Logout}" />
```

([Uno Platform][1])

**2. Call `LogoutAsync` from the viewmodel**

```csharp
public record SecondViewModel(
    IDispatcher Dispatcher,
    IAuthenticationService Auth,
    INavigator Navigator)
{
    public async Task Logout()
    {
        await Auth.LogoutAsync(Dispatcher, CancellationToken.None);

        // optional: go back to login
        await Navigator.NavigateViewModelAsync<MainViewModel>(this);
    }
}
```

Why record? The original doc uses a record for brevity, but a class is fine too. Key call is `LogoutAsync(...)`. ([Uno Platform][1])

---

## 5. Sign in by calling a backend (Refit)

**Goal**
Instead of validating “Bob” locally, call a real (or demo) API and store the returned token.

**Dependencies**

* `Uno.Extensions.Authentication`
* `Uno.Extensions.Http`
* `Refit` (brought in by the Uno HTTP extension)
* `Uno.Extensions.Navigation`

**1. Enable HTTP in the .csproj**

```xml
<PropertyGroup>
  <UnoFeatures>
    Material;
    Authentication;
    Http;
    Toolkit;
    MVUX;
  </UnoFeatures>
</PropertyGroup>
```

This adds HTTP/Refit plumbing. ([Uno Platform][1])

**2. Define the API contract**

```csharp
using Refit;
using System.Text.Json.Serialization;

[Headers("Content-Type: application/json")]
public interface IDummyJsonEndpoint
{
    [Post("/auth/login")]
    Task<AuthResponse> Login(Credentials credentials, CancellationToken ct);
}

public class Credentials
{
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("password")]
    public string? Password { get; init; }
}

public class AuthResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
```

This mirrors `https://dummyjson.com/auth/login`. ([Uno Platform][1])

**3. Register the HTTP client in the host**

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host
            .UseHttp((context, http) =>
                http.AddRefitClient<IDummyJsonEndpoint>(context)
            );
        });

    Host = builder.Build();
}
```

This tells Uno Extensions to create a Refit client from our interface. ([Uno Platform][1])

**4. Add the API base URL to `appsettings.json`**

```json
{
  "AppConfig": {
    "Title": "AuthSample"
  },
  "LocalizationConfiguration": {
    "Cultures": [ "en" ]
  },
  "DummyJsonEndpoint": {
    "Url": "https://dummyjson.com",
    "UseNativeHandler": true
  }
}
```

The section name must match the interface name **without** the leading `I` → `IDummyJsonEndpoint` → `DummyJsonEndpoint`. ([Uno Platform][1])

**5. Register authentication that uses the API**

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host
            .UseHttp((context, http) =>
                http.AddRefitClient<IDummyJsonEndpoint>(context)
            )
            .UseAuthentication(auth =>
                auth.AddCustom<IDummyJsonEndpoint>(custom =>
                    custom.Login(async (api, dispatcher, tokenCache, credentials, ct) =>
                    {
                        var username = credentials.FirstOrDefault(x => x.Key == "Username").Value;
                        var password = credentials.FirstOrDefault(x => x.Key == "Password").Value;

                        var resp = await api.Login(
                            new Credentials { Username = username, Password = password },
                            ct);

                        if (!string.IsNullOrEmpty(resp?.Token))
                        {
                            // store token in the auth dictionary
                            credentials["AccessToken"] = resp.Token;
                            return credentials;
                        }

                        return default;
                    })
                )
            );
        });

    Host = builder.Build();
}
```

Differences vs local check:

* we used `AddCustom<IDummyJsonEndpoint>` to get the API instance directly,
* we placed the token back into credentials. ([Uno Platform][1])

**6. Update the login viewmodel to send username + password**

```csharp
public class MainViewModel
{
    public string? Username { get; set; }
    public string? Password { get; set; }

    private readonly IAuthenticationService _auth;
    private readonly IDispatcher _dispatcher;
    private readonly INavigator _navigator;

    public MainViewModel(
        IDispatcher dispatcher,
        INavigator navigator,
        IAuthenticationService auth)
    {
        _dispatcher = dispatcher;
        _navigator = navigator;
        _auth = auth;
    }

    public async Task Authenticate()
    {
        var ok = await _auth.LoginAsync(
            _dispatcher,
            new Dictionary<string, string>
            {
                { nameof(Username), Username ?? string.Empty },
                { nameof(Password), Password ?? string.Empty },
            },
            CancellationToken.None);

        if (ok)
        {
            await _navigator.NavigateViewModelAsync<SecondViewModel>(this);
        }
    }
}
```

([Uno Platform][1])

**7. Update the UI with a password field**

```xml
<!-- MainPage.xaml -->
<TextBox Text="{Binding Username, Mode=TwoWay}" />
<TextBox Text="{Binding Password, Mode=TwoWay}" />
<Button Content="Login"
        Click="{x:Bind ViewModel.Authenticate}" />
```

(Use a PasswordBox on WinUI if you want masking — this example stays close to the source.) ([Uno Platform][1])

---

## 7. Minimal sequence (summary chunk for RAG)

1. **Add features:** Authentication (+ Http if using backend).
2. **Register:** `UseAuthentication(...)` with either `AddCustom(...)` or `AddCustom<TService>(...)`.
3. **UI:** TextBoxes + “Login” → call `LoginAsync(...)`.
4. **Post-login:** navigate to a protected viewmodel.
5. **Startup:** call `RefreshAsync(...)` to auto-sign-in.
6. **Logout:** button → `LogoutAsync(...)`.
7. **Backend:** add Refit client, configure base URL, pass username/password, store token.
   All parts are independent and can be shown to an LLM as separate chunks. ([Uno Platform][1])

---

[1]: https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Authentication/HowTo-Authentication.html "How-To: Get Started with Authentication "
