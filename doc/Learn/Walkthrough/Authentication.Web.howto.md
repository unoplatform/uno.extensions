---
uid: Uno.Extensions.Authentication.Web.HowTo
title: Authenticate Users with a Web View
tags: [authentication, web, navigation]
---

> **UnoFeature:** Authentication

# Let users sign in from a web view

## 1. Show a web page to sign users in

**Goal**
Open a web view, let the user sign in with your identity provider, and get redirected back to the app.

**When to use**

* You already registered the app in your identity provider (client id, redirect, etc.).
* You want to use Uno.Extensions authentication with a web flow.

Requires the `Authentication` UnoFeature.

**Steps**

1. **Enable authentication feature in your shared head**
   In your `.csproj` (the one with `<UnoFeatures>`), make sure `Authentication` is present:

   ```xml
   <UnoFeatures>
     Material;
     Authentication;
     Toolkit;
     MVUX;
   </UnoFeatures>
   ```

2. **Configure authentication in the app host**

   ```csharp
   private IHost Host { get; set; }

   protected override void OnLaunched(LaunchActivatedEventArgs args)
   {
       var builder = this.CreateBuilder(args)
           .Configure(host =>
           {
               host
                   .UseAuthentication(auth =>
                   {
                       // we'll plug the web provider right after
                       auth.AddWeb();
                   });
           });

       Host = builder.Build();
       base.OnLaunched(args);
   }
   ```

3. **Add web section to configuration**

   `appsettings.json`:

   ```json
   {
     "Web": {
       "LoginStartUri": "https://your-idp.example.com/login",
       "LogoutStartUri": "https://your-idp.example.com/logout"
     }
   }
   ```

**What happens**

* The `WebAuthenticationProvider` opens that login URL in an in-app web view.
* After the provider finishes the external login, it redirects back to the app and the provider stores the tokens.

---

## 2. Add the web authentication provider to the host

**Goal**
Tell Uno.Extensions: “use the web-based provider for auth”.

Requires the `Authentication` UnoFeature.

Configuration in `appsettings.json` must contain the `Web` section.

**Steps**

1. **Call `UseAuthentication(...)` in startup**
2. **Inside it, call `AddWeb()`**

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host
                .UseAuthentication(auth =>
                {
                    // You can add other providers too, but here it's only web:
                    auth.AddWeb();
                });
        });

    var appHost = builder.Build();
    base.OnLaunched(args);
}
```

**Notes**

* `AddWeb()` wires an `IAuthenticationProvider` backed by a web view.
* Tokens are persisted by the provider.

---

## 3. Configure login and logout URLs

**Goal**
Tell the web provider *where* to start login and *where* to start logout.

Requires the `Authentication` UnoFeature.

**Steps**

1. **Add JSON section named `Web`**

   ```json
   {
     "Web": {
       "LoginStartUri": "https://your-idp.example.com/oauth2/v2.0/authorize",
       "LogoutStartUri": "https://your-idp.example.com/oauth2/v2.0/logout"
     }
   }
   ```

2. **Make sure the app reads configuration**
   (typical Uno.Extensions template already does)

3. **Ensure your identity provider redirects back to your app**

   * Set redirect URI in the IDP
   * Use the same URI the Uno app expects

**Why separate file**
This lets RAG answer “how do I set the login url?” without reading a full tutorial.

---

## 4. Run custom code after login (process tokens)

**Goal**
Get the tokens the provider received and do something extra with them (store claims, call an API, transform token).

Requires the `Authentication` UnoFeature.

**Steps**

1. **Configure the provider with options**

   ```csharp
   protected override void OnLaunched(LaunchActivatedEventArgs args)
   {
       var builder = this.CreateBuilder(args)
           .Configure(host =>
           {
               host
                   .UseAuthentication(auth =>
                   {
                       auth.AddWeb(options =>
                       {
                           options.PostLogin(async (authService, tokens, ct) =>
                           {
                               // tokens.AccessToken
                               // tokens.RefreshToken
                               // tokens.IdToken
                               // Save extra stuff here, call an API, etc.

                               // Must return tokens
                               return tokens;
                           });
                       });
                   });
           });

       var appHost = builder.Build();
       base.OnLaunched(args);
   }
   ```

2. **Return the tokens**
   The delegate must return the final token set so the provider can persist it.

**What this enables**

* Map the identity provider user to your own user
* Download user profile right after login
* Fail the login if tokens are missing

---

## 5. Add a “Login” button to the UI

**Goal**
Show a button in XAML that triggers the web login flow through the authentication service.

Requires the `Authentication` UnoFeature.

**XAML**

```xml
<Page
    x:Class="MyApp.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:MyApp">
    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Button
            Content="Login"
            Command="{x:Bind ViewModel.LoginCommand}" />
    </Grid>
</Page>
```

**ViewModel**

```csharp
public class MainViewModel
{
    private readonly IAuthenticationService _auth;

    public MainViewModel(IAuthenticationService auth)
    {
        _auth = auth;
    }

    public ICommand LoginCommand => new AsyncRelayCommand(LoginAsync);

    private async Task LoginAsync()
    {
        // This shows the web view and goes through the configured flow
        await _auth.LoginAsync();
    }
}
```

> If you don’t already have `AsyncRelayCommand`, use your favorite command implementation.

**What happens**

* Button → command → `IAuthenticationService.LoginAsync()` → provider opens web view → user signs in → provider stores tokens.

---

## 6. Log the user out through the web provider

**Goal**
Start the remote logout and clear creds.

Requires the `Authentication` UnoFeature and `LogoutStartUri` in configuration.

**Steps**

```csharp
public class MainViewModel
{
    private readonly IAuthenticationService _auth;

    public MainViewModel(IAuthenticationService auth)
    {
        _auth = auth;
    }

    public ICommand LogoutCommand => new AsyncRelayCommand(LogoutAsync);

    private async Task LogoutAsync()
    {
        await _auth.LogoutAsync();
    }
}
```

**What happens**

* Provider navigates to the `LogoutStartUri`
* Local tokens are cleared
