---
uid: Uno.Extensions.Authentication.WebAuthentication
title: Authenticate Through a Web View
tags: [authentication, web, wab]
---
# Launch authentication through a hosted web view

`WebAuthenticationProvider` hosts a web view so users can sign in through your identity provider and return tokens to the app.

## Enable web authentication

Toggle the `Authentication` feature to include `Uno.Extensions.Authentication`.

```diff
<UnoFeatures>
    Material;
+   Authentication;
    Toolkit;
    MVUX;
</UnoFeatures>
```

## Register the web provider

Add the web authentication provider to the host pipeline.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseAuthentication(builder =>
            {
                builder.AddWeb();
            });
        });
}
```

The provider opens a web view, navigates to your login page, and stores the resulting tokens in the credential cache.

## Configure login and logout URIs

Define the endpoints used to start authentication flows.

```json
{
  "Web": {
    "LoginStartUri": "https://identity.example.com/connect/authorize",
    "LogoutStartUri": "https://identity.example.com/connect/endsession"
  }
}
```

- `LoginStartUri` launches the login flow inside the web view.
- `LogoutStartUri` signs the user out when `LogoutAsync` runs.

The identity provider should redirect back to your app using the callback URI you registered during app onboarding.

## Inspect the returned tokens

Use the `PostLogin` hook to inspect or augment the tokens before they are cached.

```csharp
builder.AddWeb(options =>
{
    options.PostLogin(async (authService, tokens, ct) =>
    {
        // Persist custom claims, telemetry, etc.
        return tokens;
    });
});
```

Return the tokens (possibly modified) so they are stored by `WebAuthenticationProvider`.

## Start the web sign-in flow from the UI

Bind a button to `LoginAsync` to trigger the web view.

```xml
<Grid>
    <Button Content="Login"
            Command="{x:Bind ViewModel.Authenticate}" />
</Grid>
```

```csharp
public class MainViewModel
{
    private readonly IAuthenticationService _authService;

    public MainViewModel(IAuthenticationService authService)
    {
        _authService = authService;
        Authenticate = new AsyncRelayCommand(AuthenticateAsync);
    }

    public IAsyncRelayCommand Authenticate { get; }

    private async Task AuthenticateAsync() =>
        await _authService.LoginAsync();
}
```

When `LoginAsync` runs, the provider navigates to `LoginStartUri`, completes the redirect back to the app, and caches the issued tokens for future HTTP calls.

## Resources

- [Authentication overview](xref:Uno.Extensions.Authentication.Overview)
- [Web Authentication Broker documentation](xref:Uno.Features.WAB)
- [Register HTTP endpoints](xref:Uno.Extensions.Http.HowToHttp)
