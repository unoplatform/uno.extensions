---
uid: Uno.Extensions.Authentication.HowToOidcAuthentication
---
# How-To: Get Started with Oidc Authentication

`OidcAuthenticationProvider` allows your users to sign in using their identities from a participating identity provider. It can wrap support for any [OpenID Connect](https://openid.net/connect/) backend, such as [IdentityServer](https://duendesoftware.com/products/identityserver) into an implementation of `IAuthenticationProvider`. This tutorial will use the OIDC authorization to validate user credentials.

## Step-by-steps

> [!IMPORTANT]
> This guide assumes you used the template wizard or `dotnet new unoapp` to create your solution. If not, it is recommended that you follow the [instructions](xref:Uno.Extensions.HowToGettingStarted) for creating an application from the template.

### 1. Prepare for OIDC authentication

- For this type of authentication, the application must already be registered with the desired identity provider. 

- A client id (and client secret) will be provided to you.

- Make sure `Uno.Extensions.Authentication.Oidc.WinUI` NuGet package is installed in your solution.

### 2. Set up OIDC authentication

- Use the `UseAuthentication()` extension method to configure the `IHostBuilder` to use an authentication provider. In our case, we will be using the `OidcAuthenticationProvider`.

    ```csharp
    private IHost Host { get; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => 
            {
                host
                .UseAuthentication(builder => 
                {
                    // Add the authentication provider here
                });
            });
    ...
    ```

- Add the `OidcAuthenticationProvider` using the `AddOidc()` extension method which configures the `IAuthenticationBuilder` to use it.

    ```csharp
    private IHost Host { get; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => 
            {
                host
                .UseAuthentication(builder => 
                {
                    builder.AddOidc();
                });
            });
    ...
    ```

- The `IAuthenticationBuilder` is responsible for managing the lifecycle of the associated provider that was built.

- Because it is configured to use OpenID Connect, the user will eventually be prompted to sign in to their identity provider account when they use your application. `OidcAuthenticationProvider` will then store the user's access token in credential storage. The token will be automatically refreshed when it expires.

### 3. Configure the OidcAuthenticationProvider

- While the `AddOidc()` extension method will configure the `IAuthenticationBuilder` to use the `OidcAuthenticationProvider`, it will not configure the provider itself. The `OidcAuthenticationProvider` can be configured using a configuration section. 

- We will use the default section name `Oidc` in this example:

    ```json
    {
        "Oidc": {
            "Authority": "https://demo.duendesoftware.com/",
            "ClientId": "interactive.confidential",
            "ClientSecret": "secret",
            "Scope": "openid profile email api offline_access",
            "RedirectUri": "oidc-auth://callback",
        }
    }
    ```

- `Authority`: The URL of the identity provider. 

- `ClientId` and `ClientSecret`: The client ID and client secret that were provided to you. 

- `Scope`: The scope of the access token. 

- `RedirectUri`: The URL that the identity provider will redirect to after the user has authenticated.

### 4. Use the provider in your application

- Update the `MainPage` to include a button that will allow the user to sign in with your desired service.

    ```xml
    <Page
        x:Class="UnoOidcAuthentication.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:UnoOidcAuthentication"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d">

        <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
            <Button x:Name="SignInButton" Content="Sign In" Command="{x:Bind ViewModel.Authenticate}" />
        </Grid>
    </Page>
    ```

- Update the `MainViewModel` to include a command that will allow the user to sign in with your desired service.

    ```csharp
    public class MainViewModel : ObservableObject
    {
        private readonly IAuthenticationService _authenticationService;

        public MainViewModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public ICommand Authenticate => new AsyncRelayCommand(AuthenticateImpl);

        private async Task AuthenticateImpl()
        {
            await _authenticationService.LoginAsync(/* ... */);
        }
    }
    ```

- Finally, we can pass the login credentials to the `LoginAsync()` method and authenticate with the identity provider. The user will be prompted to sign in to their account when they tap the button in the application. 

- `OidcAuthenticationProvider` will then store the user's access token in credential storage. The token will be automatically refreshed when it expires.