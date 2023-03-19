---
uid: Overview.Authentication.Custom
---
# Custom

The `CustomAuthenticationProvider` provides a basic implementation of the `IAuthenticationProvider` that requires callback methods to be defined for performing login, refresh and logout actions. This is the most flexible provider as it allows the application to define the authentication process. It is typically used when the application is using a custom authentication service that isn't supported by one of the other providers, and is included in the Uno.Extensions.Authentication NuGet package.

## Set up custom authentication

The `CustomAuthenticationProvider` is added using the `AddCustom()` extension method which configures the `IAuthenticationBuilder` to use it. The following example shows how to configure the provider:

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
                builder.AddCustom(());
            });
        });
...
```

## 