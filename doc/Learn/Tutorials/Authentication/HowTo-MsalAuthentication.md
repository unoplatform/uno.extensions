---
uid: Learn.Tutorials.Authentication.HowToMsalAuthentication
---
# How-To: Get Started with MSAL Authentication

`MsalAuthenticationProvider` allows your users to sign in using their Microsoft identities. It wraps the [MSAL library](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) from Microsoft into an implementation of `IAuthenticationProvider` This tutorial will use MSAL authorization to validate user credentials.

## Step-by-steps

> [!IMPORTANT]
> This guide assumes you used the template wizard or `dotnet new unoapp` to create your solution. If not, it is recommended that you follow the [instructions](xref:Overview.Extensions) for creating an application from the template.

### 1. Prepare for MSAL authentication

- For this type of authentication, the application must be registered with the Microsoft identity platform. For more information, see [Register an application with the Microsoft identity platform](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app).

- The identity provider will provider you with a client ID and guidance on scopes to use.

- Make sure `Uno.Extensions.Authentication.MSAL.WinUI` NuGet package is installed in your solution.

### 2. Set up MSAL authentication

- Use the `UseAuthentication()` extension method to configure the `IHostBuilder` to use an authentication provider. In our case, we will be using the `MsalAuthenticationProvider`.

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

- Add the `MsalAuthenticationProvider` using the `AddMsal()` extension method which configures the `IAuthenticationBuilder` to use it.

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
                    builder.AddMsal();
                });
            });
    ...
    ```

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