---
uid: Overview.Authentication.Msal
---
# MSAL

The `MsalAuthenticationProvider` allows users to sign in using their Microsoft identities. It wraps the [MSAL library](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) from Microsoft into an implementation of `IAuthenticationProvider`, and is included with the Uno.Extensions.Authentication.Msal.WinUI or Uno.Extensions.Authentication.Msal.UI NuGet packages. This implementation invokes the required web based authentication process.

## Register for MSAL authentication

For this type of authentication, you first need to register your application with the Microsoft identity platform. For more information, see [Register an application with the Microsoft identity platform](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app).

## Set up MSAL authentication

To use MSAL authentication, you need to provide the following information:

- Client id
- Scopes
- Storage (optional)

The `MsalAuthenticationProvider` is added using the `AddMsal()` extension method which configures the `IAuthenticationBuilder` to use it. The following example shows how to configure the provider:

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
                builder
                .AddMsal(msal => 
                {
                    msal
                    .Scopes(/* Scopes */)
                    .Storage(new Microsoft.Identity.Client.Extensions.Msal.StorageCreationPropertiesBuilder()
                        .WithMacKeyChain(/* ... */)
                        .WithLinuxKeyring(/* ... */)
                        .WithWindowsCredMan(/* ... */)
                    )
                    .Builder(Microsoft.Identity.Client.PublicClientApplicationBuilder.Create(/* Client Id */))
                });
            });
        });
...
```

The `IAuthenticationBuilder` is responsible for managing the lifecycle of the associated provider that was built. 
Because it is configured to use Msal, the user will be prompted to sign in to their Microsoft account when they launch the application. `MsalAuthenticationProvider` will then store the user's access token in the provided storage. The token will be automatically refreshed when it expires.