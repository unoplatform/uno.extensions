---
uid: Uno.Extensions.Authentication.MsalAuthentication
title: Sign In with Microsoft Accounts
tags: [authentication, msal, microsoft-identity]
---
# Let users sign in with Microsoft identities

Wrap the Microsoft Identity platform with `MsalAuthenticationProvider` so users can authenticate with their Microsoft account and keep tokens refreshed automatically.

> [!IMPORTANT]
> Register your application in Azure Active Directory before starting. Follow [Register an application with the Microsoft identity platform](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app) to obtain a client ID and list of scopes.

## Enable MSAL support

Add the `AuthenticationMsal` feature to reference `Uno.Extensions.Authentication.Msal`.

```diff
<UnoFeatures>
    Material;
+   AuthenticationMsal;
    Toolkit;
    MVUX;
</UnoFeatures>
```

## Register the MSAL provider with a window

Call `AddMsal` inside `UseAuthentication`, supplying the current `Window`.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure((host, window) =>
        {
            host.UseAuthentication(builder =>
            {
                builder.AddMsal(window);
            });
        });
}
```

`AddMsal` needs a `Window` so the provider can display the Microsoft login dialog. Omitting it results in an `MsalClientException` referencing loopback redirect URIs.

## Configure client ID and scopes

Provide the MSAL configuration in `appsettings.json`, or inline when calling `AddMsal`.

```json
{
  "Msal": {
    "ClientId": "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b",
    "Scopes": [ "User.Read", "Tasks.Read" ]
  }
}
```

```csharp
builder.AddMsal(window, msal =>
    msal
        .Builder(msalBuilder =>
            msalBuilder.WithClientId("161a9fb5-3b16-487a-81a2-ac45dcc0ad3b"))
        .Scopes(new[] { "User.Read", "Tasks.Read" }));
```

> [!WARNING]
> MSAL requires a valid GUID client ID. If the provider cannot locate one, `MsalAuthenticationProvider` throws `Microsoft.Identity.Client.MsalClientException: No ClientId was specified.`

## Start the Microsoft sign-in flow

Inject `IAuthenticationService` into your view model and trigger `LoginAsync` from the UI.

```xml
<Grid>
    <Button Content="Sign in with Microsoft"
            Command="{x:Bind ViewModel.Authenticate}" />
</Grid>
```

```csharp
public class MainViewModel
{
    private readonly IAuthenticationService _authenticationService;

    public MainViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        Authenticate = new AsyncRelayCommand(AuthenticateAsync);
    }

    public IAsyncRelayCommand Authenticate { get; }

    private async Task AuthenticateAsync() =>
        await _authenticationService.LoginAsync();
}
```

When the command runs, `MsalAuthenticationProvider` prompts for a Microsoft account, caches the returned tokens, and silently refreshes them on subsequent requests.

## Resources

- [Authentication overview](xref:Uno.Extensions.Authentication.Overview)
- [Azure AD app registration quickstart](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app)
- [MSAL .NET library](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet)
