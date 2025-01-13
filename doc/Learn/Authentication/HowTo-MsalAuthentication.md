---
uid: Uno.Extensions.Authentication.HowToMsalAuthentication
---
# How-To: Get Started with MSAL Authentication

`MsalAuthenticationProvider` allows your users to sign in using their Microsoft identities. It wraps the [MSAL library](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) from Microsoft into an implementation of `IAuthenticationProvider`. This tutorial will use MSAL authorization to validate user credentials.

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Prepare for MSAL authentication

- For this type of authentication, the application must be registered with the Microsoft identity platform. For more information, see [Register an application with the Microsoft identity platform](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app).

- The identity provider will provider you with a client ID and guidance on scopes to use.

- Add `AuthenticationMsal` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

    ```diff
    <UnoFeatures>
        Material;
    +   AuthenticationMsal;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

### 2. Set up MSAL authentication

- Use the `UseAuthentication()` extension method to configure the `IHostBuilder` to use an authentication provider. In our case, we will be using the `MsalAuthenticationProvider`.

    ```csharp
    private IHost Host { get; set; }

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
    }
    ```

- Use the `Configure` method overload that provides access to a `Window` instance. Add the `MsalAuthenticationProvider` using the `AddMsal()` extension method which configures the `IAuthenticationBuilder` to use it.

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure((host, window) =>
            {
                host
                .UseAuthentication(builder =>
                {
                    builder.AddMsal(window);
                });
            });
        ...
    }
    ```

> [!IMPORTANT]
> The `AddMsal()` method requires a `Window` instance, which the `MsalAuthenticationProvider` uses to set up the authentication dialog. You can access the `Window` instance through the `Configure()` method overload that provides it.
> **Note:** Failing to pass a valid `Window` instance could result in a `MsalClientException` with the message:
> *"Only loopback redirect uri is supported, but <your_redirect_uri> was found. Configure http://localhost or http://localhost:port both during app registration and when you create the PublicClientApplication object. See https://aka.ms/msal-net-os-browser for details."*

- The `IAuthenticationBuilder` is responsible for managing the lifecycle of the associated provider that was built.

- Because it is configured to use MSAL, the user will eventually be prompted to sign in to their Microsoft account when they use your application. `MsalAuthenticationProvider` will then store the user's access token in credential storage. The token will be automatically refreshed when it expires.

### 3. Configure the provider

- While `MsalAuthenticationProvider` is added using the `AddMsal()` extension method, you will need to add a configuration section to your appsettings.json file with your client ID and scopes.

    The following example shows how to configure the provider using the default section name:

    ```json
    {
      "Msal": {
        "ClientId": "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b",
        "Scopes": [ "Tasks.Read", "User.Read", "Tasks.ReadWrite" ]
      }
    }
    ```

    This configuration can also be done in the root App.cs file:

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure((host, window) =>
            {
                host
                .UseAuthentication(builder =>
                {
                    builder.AddMsal(window, msal =>
                        msal
                        .Builder(msalBuilder => 
                            msalBuilder.WithClientId("161a9fb5-3b16-487a-81a2-ac45dcc0ad3b"))
                        .Scopes(new[] { "Tasks.Read", "User.Read", "Tasks.ReadWrite" })
                    );
                });
            });
        ...
    }
    ```

    > [!WARNING]
    > A ClientId of GUID format is required for MSAL Authentication to work. You can specify it in the appsettings.json file, or in the code itself.
    > If the ClientId cannot be found, the app will crash with the following error:

    ```xml
    Exception thrown: 'Microsoft.Identity.Client.MsalClientException' in Microsoft.Identity.Client.dll
    No ClientId was specified.
    ```

### 4. Use the provider in your application

- Update the `MainPage` to include a `Button` labeled to sign in with Microsoft.

    ```xml
    <Page
        x:Class="MyApp.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:MyApp"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d">

        <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
            <Button x:Name="SignInButton" Content="Sign in with Microsoft" Command="{x:Bind ViewModel.Authenticate}" />
        </Grid>
    </Page>
    ```

- Because the `IAuthenticationService` instance will be injected into our view models, we can now update the `MainViewModel` to include a `Command` that will use that service to sign in a user.

    ```csharp
    public class MainViewModel : ObservableObject
    {
        private readonly IAuthenticationService _authenticationService;

        public MainViewModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
            Authenticate = new AsyncRelayCommand(AuthenticateAsync);
        }

        public ICommand Authenticate { get; }

        private async Task AuthenticateAsync()
        {
            await _authenticationService.LoginAsync(/* ... */);
        }
    }
    ```

- Finally, we can run the application and sign in with our Microsoft account. The user will be prompted to sign in to their Microsoft account when they tap the button in the application.

- `MsalAuthenticationProvider` will then store the user's access token in credential storage. The token will be automatically refreshed when it expires.
