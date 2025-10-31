
# Sign in with Microsoft account (MSAL)

This how-to shows how to let users sign in with their Microsoft identity in an Uno Platform app using **Uno.Extensions** and **MSAL**.

> Depends on NuGet:

> - Uno.Extensions.Authentication.Msal.WinUI (or the meta package that brings it in)
> - Microsoft.Identity.Client (transitive)

## 1. Register the app in Entra ID

- Go to the Microsoft identity platform and create an app registration.
- Copy the **Application (client) ID**.
- Add **Redirect URI**: `http://localhost` or `http://localhost:{port}`.

(Why? Uno’s MSAL provider will throw if the redirect isn’t loopback, same as MSAL itself.)

## 2. Turn on MSAL in the Uno features

In your **.csproj**:

```xml
<PropertyGroup>
  <UnoFeatures>
    Material;
    AuthenticationMsal;
    Toolkit;
    MVUX;
  </UnoFeatures>
</PropertyGroup>
````

This makes sure the host knows MSAL is a thing.

## 3. Add MSAL to the host

In **App.xaml.cs** (or your entry point):

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure((host, window) =>
        {
            host
                .UseAuthentication(auth =>
                {
                    // must pass window
                    auth.AddMsal(window);
                });
        });

    Host = builder.Build();
}
```

**Important:** `AddMsal(...)` needs the **Window**. If you don’t pass it, MSAL cannot show the system browser dialog and you can get `MsalClientException` about loopback URIs.

## 4. Add configuration

In **appsettings.json**:

```json
{
  "Msal": {
    "ClientId": "00000000-0000-0000-0000-000000000000",
    "Scopes": [ "User.Read" ]
  }
}
```

You can also do it in code (see separate how-to).

## 5. Show a sign-in button

In **MainPage.xaml**:

```xml
<Page
    x:Class="MyApp.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Button
            Content="Sign in with Microsoft"
            Command="{x:Bind ViewModel.Authenticate}" />
    </Grid>
</Page>
```

## 6. Call the authentication service

In **MainViewModel.cs**:

```csharp
public class MainViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;

    public MainViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        Authenticate = new AsyncRelayCommand(AuthenticateAsync);
    }

    public IAsyncRelayCommand Authenticate { get; }

    private async Task AuthenticateAsync()
    {
        await _authenticationService.LoginAsync();
    }
}
```

This triggers the MSAL flow. Token is cached and refreshed automatically.

[1]: https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Authentication/HowTo-MsalAuthentication.html "How-To: Get Started with MSAL Authentication "
