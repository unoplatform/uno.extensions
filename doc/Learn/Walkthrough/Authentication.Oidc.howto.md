
# Let users sign in with OIDC

**Goal:** enable sign-in against an OpenID Connect (OIDC) provider in an Uno.Extensions app.

**Dependencies**

```xml

* NuGet: `Uno.Extensions.Authentication.Oidc.WinUI` (or the package that brings OIDC for your template)
* In your shared `.csproj`, add the feature:

```xml
<UnoFeatures>
  Material;
  AuthenticationOidc;
  Toolkit;
  MVUX;
</UnoFeatures>
```

**Steps**

1. In `App.xaml.cs` (or your `App`), create the host and turn on authentication:

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
                    auth.AddOidc(); // 👈 use OIDC provider
                });
        });

    Host = builder.Build();
}
```

2. The app now knows how to authenticate using OIDC. The actual provider configuration is done in app settings (see next how-to).

**What this gives you**

* Uses `OidcAuthenticationProvider` behind the scenes. ([Uno Platform][1])
* Tokens are stored and refreshed for you. ([Uno Platform][1])

---

# 2. Store OIDC settings in configuration

**Goal:** configure the OIDC provider (authority, client id, scopes) from `appsettings.json` (or equivalent).

**Steps**

1. Add a section named **`Oidc`**:

```json
{
  "Oidc": {
    "Authority": "https://demo.duendesoftware.com/",
    "ClientId": "interactive.confidential",
    "ClientSecret": "secret",
    "Scope": "openid profile email api offline_access",
    "RedirectUri": "oidc-auth://callback"
  }
}
```

**Important fields**

* `Authority`: URL of your identity provider. ([Uno Platform][1])
* `ClientId`, `ClientSecret`: from your app registration. ([Uno Platform][1])
* `Scope`: include `openid` at minimum; add `profile`, `email`, `offline_access` if needed. ([Uno Platform][1])
* `RedirectUri`: must match what the IdP knows.

2. Make sure your host builder loads configuration (the Uno.Extensions template already does).

**Why a fixed name?**

* `AddOidc()` looks for the `Oidc` section by default, so you don’t have to pass options manually. ([Uno Platform][1])

---

# 3. Generate redirect URI automatically

**Goal:** avoid hardcoding `RedirectUri` and let the platform give it to you.

**When to use:** WASM already does this by default; other targets can opt in. ([Uno Platform][1])

**Steps**

```csharp
host
    .UseAuthentication(auth =>
    {
        auth.AddOidc()
            .AutoRedirectUriFromAuthenticationBroker(); // 👈 takes URI from WAB
    });
```

**What it does**

* Uses `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` to set redirect + post-logout URIs. ([Uno Platform][1])
* Overrides whatever was in configuration.

**Use this when**

* You ship on multiple platforms
* You don’t want to keep URIs in JSON

---

# 4. Show a “Sign in” button in XAML

**Goal:** let the user tap a button and run the authentication flow.

**XAML**

```xml
<Page
    x:Class="MyApp.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Button
            Content="Sign in"
            Command="{x:Bind ViewModel.Authenticate}" />
    </Grid>
</Page>
```

**ViewModel**

```csharp
public partial class MainViewModel
{
    private readonly IAuthenticationService _auth;

    public MainViewModel(IAuthenticationService auth)
    {
        _auth = auth;
    }

    public IAsyncRelayCommand Authenticate => new AsyncRelayCommand(SignIn);

    private async Task SignIn()
    {
        await _auth.LoginAsync(); // provider is OIDC
    }
}
```

**What happens**

* Uno.Extensions resolves `IAuthenticationService`.
* `LoginAsync()` starts the OIDC flow.
* The OIDC provider stores the tokens and refreshes them later. ([Uno Platform][1])

---

# 5. Customize OIDC client options

**Goal:** tweak low-level OIDC behavior (discovery, PAR, policies).

**Steps**

```csharp
host
    .UseAuthentication(auth =>
    {
        auth.AddOidc()
            .ConfigureOidcClientOptions(options =>
            {
                // toggle PAR
                options.DisablePushedAuthorization = false;

                // require ID token on refresh
                options.Policy.RequireIdentityTokenOnRefreshTokenResponse = true;

                // skip issuer name validation (only if you know what you’re doing)
                options.Policy.Discovery.ValidateIssuerName = false;
            });
    });
```

**When to do this**

* Your IdP needs slightly different discovery
* You need stricter refresh-token rules
* You are working with a dev/preview IdP

**Source:** the underlying provider uses `IdentityModel.OidcClient`, so these settings map to that library. ([Uno Platform][1])

---

# 6. Use a custom browser for sign-in

**Goal:** control how the sign-in UI is shown (in-app, system browser, custom wrapper).

**Steps**

1. Create a browser implementing `IdentityModel.OidcClient.IBrowser`:

```csharp
public class CustomBrowser : IBrowser
{
    public Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken ct = default)
    {
        // open your own WebView / window
        // return BrowserResult with response
        throw new NotImplementedException();
    }
}
```

2. Register it in app builder:

```csharp
.ConfigureServices((context, services) =>
{
    services.AddTransient<IBrowser, CustomBrowser>();
})
```

**Why this works**

* The OIDC provider asks DI for `IBrowser`
* Your implementation is used to complete the auth flow ([Uno Platform][1])

---

# 7. Make sure the app is registered in the IdP

**Goal:** ensure the OIDC flow can complete.

**Checklist**

* App is registered in the identity provider
* You have **client id** (and secret if confidential client) ([Uno Platform][1])
* Redirect URI in the IdP == redirect URI in your app
* Scopes in the app match the IdP

If any of these are missing, the OIDC provider in Uno.Extensions will start but the IdP will fail the flow.

---

# 8. Quick FAQ (RAG-friendly)

**Q: Where do I turn on authentication?**
A: In the host builder: `.UseAuthentication(auth => auth.AddOidc());` ([Uno Platform][1])

**Q: Where do I put OIDC JSON?**
A: In config under `"Oidc": { ... }` (the provider looks for that). ([Uno Platform][1])

**Q: Do I need to pass login parameters to `LoginAsync()`?**
A: Not for the basic flow; config is enough.

**Q: Will tokens refresh?**
A: Yes, the OIDC provider stores and refreshes tokens. ([Uno Platform][1])

**Q: Can I do platform-specific redirect URIs?**
A: Yes—use `.AutoRedirectUriFromAuthenticationBroker()` so the platform decides. ([Uno Platform][1])

---

[1]: https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Authentication/HowTo-OidcAuthentication.html "How-To: Get Started with Oidc Authentication "
