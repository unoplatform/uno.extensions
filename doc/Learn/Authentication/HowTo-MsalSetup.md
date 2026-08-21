---
uid: Uno.Extensions.Authentication.HowToMsalSetup
---
# Complete MSAL Authentication Setup Guide

> **UnoFeatures:** `AuthenticationMsal` (add to `<UnoFeatures>` in your `.csproj`)

This comprehensive guide walks you through setting up Microsoft Authentication Library (MSAL) authentication in your Uno Platform application, from Azure AD configuration to application implementation.

## Overview

MSAL authentication allows users to sign in with Microsoft work/school accounts or personal Microsoft accounts. This guide covers:

1. Azure AD app registration and configuration
2. Uno Platform project setup
3. Configuration options
4. Token retrieval and usage
5. Platform-specific considerations

## Part 1: Azure AD App Registration

### Step 1: Create App Registration

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Go to **Azure Active Directory** > **App registrations**
3. Click **New registration**
4. Configure your app:
   - **Name**: Your application name (e.g., "My Uno App")
   - **Supported account types**: Choose based on your requirements:
     - **Accounts in this organizational directory only**: Single tenant (most restrictive)
     - **Accounts in any organizational directory**: Multi-tenant (work/school accounts)
     - **Accounts in any organizational directory and personal Microsoft accounts**: Multi-tenant + personal (most permissive)
   - **Redirect URI**: Leave blank for now (we'll add it later)
5. Click **Register**
6. **Copy the Application (client) ID** - you'll need this for your app configuration

### Step 2: Configure Platform Settings

1. In your app registration, go to **Authentication**
2. Click **Add a platform**
3. Select **Mobile and desktop applications**
4. Add redirect URIs:
   - `http://localhost` (required for desktop/mobile)
   - `http://localhost:5000` (optional, if using a specific port)
   - For custom URIs, see platform-specific sections below
5. Under **Advanced settings**:
   - Enable **Public client flows**: Set "Allow public client flows" to **Yes**
   - This is critical for mobile and desktop apps
6. Click **Configure**

**Verification with Azure CLI:**

```bash
az ad app show --id <your-client-id> --query "{signInAudience:signInAudience, publicClient:publicClient, redirectUris:publicClient.redirectUris}"
```

Expected output:

```json
{
  "publicClient": {
    "redirectUris": [
      "http://localhost"
    ]
  },
  "redirectUris": [
    "http://localhost"
  ],
  "signInAudience": "AzureADandPersonalMicrosoftAccount"
}
```

### Step 3: Configure API Permissions

1. In your app registration, go to **API permissions**
2. By default, **Microsoft Graph > User.Read** is added
3. Add additional permissions as needed:
   - Click **Add a permission**
   - Select **Microsoft Graph**
   - Choose **Delegated permissions**
   - Common permissions:
     - `User.Read` - Read user profile (default)
     - `User.ReadBasic.All` - Read all users' basic profiles
     - `Mail.Read` - Read user mail
     - `Calendars.Read` - Read user calendars
     - `Files.Read` - Read user files
     - `offline_access` - Maintain access to data (enables refresh tokens)
4. Click **Add permissions**

**For organizational apps requiring admin consent:**
5. Click **Grant admin consent for \<Your Organization\>**
6. Confirm the consent

**Verification with Azure CLI:**

```bash
az ad app permission list --id <your-client-id>
```

### Step 4: Note Your Configuration Values

Record these values from your app registration:

- **Application (client) ID**: Found on the Overview page
- **Directory (tenant) ID**: Found on the Overview page (for single-tenant apps)
- **Redirect URI(s)**: What you configured in Authentication
- **Scopes**: Based on API permissions you added

## Part 2: Uno Platform Project Setup

### Step 1: Add AuthenticationMsal UnoFeature

Edit your `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <UnoFeatures>
      Material;
      AuthenticationMsal;
      Toolkit;
      MVUX;
    </UnoFeatures>
  </PropertyGroup>
</Project>
```

This automatically adds the required NuGet packages:

- `Uno.Extensions.Authentication.MSAL`
- `Microsoft.Identity.Client`

### Step 2: Create or Update appsettings.json

Create `appsettings.json` in your project root:

```json
{
  "Msal": {
    "ClientId": "00000000-0000-0000-0000-000000000000",
    "Scopes": [
      "User.Read",
      "offline_access"
    ]
  }
}
```

**Ensure the file is included in your project:**

Update `.csproj`:

```xml
<ItemGroup>
  <Content Include="appsettings*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### Step 3: Configure App.xaml.cs

Update your `App.xaml.cs` to add MSAL authentication:

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Uno.Extensions.Authentication;

namespace MyUnoApp;

public partial class App : Application
{
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            // Enable configuration from appsettings.json
            .UseConfiguration()
            // Add navigation support
            .UseNavigation()
            // Add MSAL authentication - MUST pass window
            .Configure((host, window) =>
            {
                host.UseAuthentication(auth =>
                {
                    auth.AddMsal(window);
                });
            });

        Host = builder.Build();

        // Continue with normal app initialization
        MainWindow = Host.Services.GetRequiredService<MainWindow>();
        MainWindow.Activate();
    }
}
```

**Critical:** The `window` parameter must be passed to `AddMsal()`. Use the `Configure((host, window) => ...)` overload.

## Part 3: Complete Configuration Options

### appsettings.json Structure

Here's the complete structure with all available options:

```json
{
  "Msal": {
    // Required: Application (client) ID from Azure AD
    "ClientId": "00000000-0000-0000-0000-000000000000",
    
    // Required: Scopes to request during authentication
    "Scopes": [
      "User.Read",
      "offline_access"
    ],
    
    // Optional: Authority URL (defaults to common)
    // For single tenant: "https://login.microsoftonline.com/{tenant-id}"
    // For multi-tenant orgs: "https://login.microsoftonline.com/organizations"
    // For all accounts: "https://login.microsoftonline.com/common"
    "Authority": "https://login.microsoftonline.com/common",
    
    // Optional: Redirect URI (defaults to http://localhost)
    "RedirectUri": "http://localhost",
    
    // Optional (iOS): Keychain security group
    "KeychainSecurityGroup": "com.yourcompany.yourapp",
    
    // Optional: Tenant ID (alternative to setting Authority)
    "TenantId": "00000000-0000-0000-0000-000000000000",
    
    // Optional: Instance (defaults to https://login.microsoftonline.com/)
    "Instance": "https://login.microsoftonline.com/"
  }
}
```

### Environment-Specific Configuration

Create `appsettings.development.json` for development settings:

```json
{
  "Msal": {
    "ClientId": "dev-client-id",
    "Authority": "https://login.microsoftonline.com/common",
    "Scopes": [
      "User.Read",
      "offline_access"
    ]
  }
}
```

And `appsettings.production.json` for production:

```json
{
  "Msal": {
    "ClientId": "prod-client-id",
    "Authority": "https://login.microsoftonline.com/{prod-tenant-id}",
    "Scopes": [
      "User.Read",
      "Mail.Read",
      "offline_access"
    ]
  }
}
```

Update `.csproj` to include environment-specific files:

```xml
<ItemGroup>
  <Content Include="appsettings*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### Code-Based Configuration

You can also configure MSAL directly in code (overrides appsettings.json):

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure((host, window) =>
        {
            host.UseAuthentication(auth =>
            {
                auth.AddMsal(window, msal => msal
                    // Configure the PublicClientApplicationBuilder
                    .Builder(builder => builder
                        .WithClientId("your-client-id")
                        .WithAuthority("https://login.microsoftonline.com/common")
                        .WithRedirectUri("http://localhost")
                    )
                    // Configure scopes
                    .Scopes(new[] { "User.Read", "offline_access" })
                );
            });
        });

    Host = builder.Build();
}
```

### Advanced Configuration

For advanced scenarios:

```csharp
auth.AddMsal(window, msal => msal
    .Builder(builder => builder
        .WithClientId("your-client-id")
        .WithAuthority("https://login.microsoftonline.com/organizations")
        .WithRedirectUri("http://localhost")
        // Enable detailed logging (development only)
        .WithLogging((level, message, containsPii) =>
        {
            Console.WriteLine($"[MSAL {level}] {message}");
        }, LogLevel.Verbose, enablePiiLogging: false)
        // iOS-specific: Configure keychain
        .WithIosKeychainSecurityGroup("com.yourcompany.yourapp")
    )
    .Scopes(new[] { "User.Read", "Mail.Read", "offline_access" })
);
```

## Part 4: Implementing Authentication in Your App

### Create a Login Page

**LoginPage.xaml:**

```xml
<Page
    x:Class="MyUnoApp.LoginPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <StackPanel 
            HorizontalAlignment="Center" 
            VerticalAlignment="Center"
            Spacing="20">
            
            <TextBlock 
                Text="Welcome to My Uno App" 
                Style="{StaticResource TitleTextBlockStyle}"
                HorizontalAlignment="Center" />
            
            <TextBlock 
                Text="Please sign in to continue" 
                Style="{StaticResource BodyTextBlockStyle}"
                HorizontalAlignment="Center" />
            
            <Button 
                Content="Sign in with Microsoft" 
                Command="{x:Bind ViewModel.SignIn}"
                HorizontalAlignment="Center"
                Padding="40,12" />
        </StackPanel>
    </Grid>
</Page>
```

**LoginViewModel.cs:**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Authentication;
using Uno.Extensions.Navigation;

namespace MyUnoApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _auth;
    private readonly INavigator _navigator;

    public LoginViewModel(
        IAuthenticationService authenticationService,
        INavigator navigator)
    {
        _auth = authenticationService;
        _navigator = navigator;
    }

    [RelayCommand]
    private async Task SignIn(CancellationToken ct)
    {
        try
        {
            var result = await _auth.LoginAsync(
                dispatcher: null,
                credentials: null,
                provider: null,
                cancellationToken: ct
            );

            if (result)
            {
                // Navigate to main page on successful login
                await _navigator.NavigateViewModelAsync<MainViewModel>(
                    this, 
                    qualifier: Qualifiers.ClearBackStack
                );
            }
            else
            {
                // Handle login failure
                await ShowErrorAsync("Login failed. Please try again.");
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions
            await ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    private async Task ShowErrorAsync(string message)
    {
        // Implement error display (e.g., ContentDialog)
        Console.WriteLine(message);
    }
}
```

### Check Authentication Status

Check if a user is already authenticated (e.g., on app startup):

```csharp
public partial class AppViewModel : ObservableObject
{
    private readonly IAuthenticationService _auth;
    private readonly INavigator _navigator;

    public AppViewModel(
        IAuthenticationService authenticationService,
        INavigator navigator)
    {
        _auth = authenticationService;
        _navigator = navigator;
        
        _ = CheckAuthenticationAsync();
    }

    private async Task CheckAuthenticationAsync()
    {
        try
        {
            // Try to refresh existing tokens
            var isAuthenticated = await _auth.RefreshAsync();

            if (isAuthenticated)
            {
                // User is authenticated, go to main page
                await _navigator.NavigateViewModelAsync<MainViewModel>(this);
            }
            else
            {
                // User is not authenticated, go to login page
                await _navigator.NavigateViewModelAsync<LoginViewModel>(this);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth check failed: {ex.Message}");
            await _navigator.NavigateViewModelAsync<LoginViewModel>(this);
        }
    }
}
```

### Sign Out

Implement sign-out functionality:

```csharp
[RelayCommand]
private async Task SignOut(CancellationToken ct)
{
    try
    {
        var result = await _auth.LogoutAsync(
            dispatcher: null,
            cancellationToken: ct
        );

        if (result)
        {
            // Navigate back to login page
            await _navigator.NavigateViewModelAsync<LoginViewModel>(
                this,
                qualifier: Qualifiers.ClearBackStack
            );
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Logout failed: {ex.Message}");
    }
}
```

## Part 5: Token Retrieval and Usage

### Accessing Tokens

To call Microsoft Graph or other APIs, you need to retrieve the access token:

```csharp
using Uno.Extensions.Authentication;

public class UserService
{
    private readonly ITokenCache _tokenCache;

    public UserService(ITokenCache tokenCache)
    {
        _tokenCache = tokenCache;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        var tokens = await _tokenCache.GetAsync(ct);
        
        if (tokens != null && tokens.TryGetValue("AccessToken", out var accessToken))
        {
            return accessToken;
        }

        return null;
    }

    public async Task<string?> GetIdTokenAsync(CancellationToken ct = default)
    {
        var tokens = await _tokenCache.GetAsync(ct);
        
        if (tokens != null && tokens.TryGetValue("IdToken", out var idToken))
        {
            return idToken;
        }

        return null;
    }
}
```

### Token Key Naming Conventions

The token cache stores tokens with these keys:

- `"AccessToken"` - OAuth 2.0 access token for calling APIs
- `"IdToken"` - OpenID Connect ID token containing user claims
- `"RefreshToken"` - Refresh token (if `offline_access` scope is granted)

### Calling Microsoft Graph API

#### Option 1: Using Microsoft Graph SDK

Add NuGet package: `Microsoft.Graph`

```csharp
using Microsoft.Graph;
using Microsoft.Graph.Models;

public class GraphService
{
    private readonly ITokenCache _tokenCache;

    public GraphService(ITokenCache tokenCache)
    {
        _tokenCache = tokenCache;
    }

    public async Task<User?> GetMyProfileAsync(CancellationToken ct = default)
    {
        var tokens = await _tokenCache.GetAsync(ct);
        
        if (tokens == null || !tokens.TryGetValue("AccessToken", out var accessToken))
        {
            return null;
        }

        var graphClient = new GraphServiceClient(
            new DelegateAuthenticationProvider(async (request) =>
            {
                request.Headers.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                await Task.CompletedTask;
            })
        );

        return await graphClient.Me.GetAsync(cancellationToken: ct);
    }
}
```

#### Option 2: Using HttpClient with Token

```csharp
using System.Net.Http.Headers;

public class GraphService
{
    private readonly ITokenCache _tokenCache;
    private readonly HttpClient _httpClient;

    public GraphService(ITokenCache tokenCache, HttpClient httpClient)
    {
        _tokenCache = tokenCache;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
    }

    public async Task<string> GetMyProfileJsonAsync(CancellationToken ct = default)
    {
        var tokens = await _tokenCache.GetAsync(ct);
        
        if (tokens == null || !tokens.TryGetValue("AccessToken", out var accessToken))
        {
            throw new InvalidOperationException("Not authenticated");
        }

        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync("me", ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
```

#### Option 3: Using Uno.Extensions.Http with Automatic Token Injection

Configure HTTP service with authentication in `App.xaml.cs`:

```csharp
builder
    .UseHttp((context, services) =>
    {
        services.AddRefitClient<IGraphApi>(context)
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
            })
            // Automatically add Authorization header with access token
            .AddHttpMessageHandler(sp => 
                sp.GetRequiredService<AuthenticationHeaderHandler>());
    });
```

Define your API interface:

```csharp
using Refit;

public interface IGraphApi
{
    [Get("/me")]
    Task<UserProfile> GetMyProfileAsync(CancellationToken ct = default);
}
```

Use in your service:

```csharp
public class UserProfileService
{
    private readonly IGraphApi _graphApi;

    public UserProfileService(IGraphApi graphApi)
    {
        _graphApi = graphApi;
    }

    public async Task<UserProfile> GetProfileAsync(CancellationToken ct = default)
    {
        // Token is automatically added to request
        return await _graphApi.GetMyProfileAsync(ct);
    }
}
```

### Token Refresh

MSAL automatically handles token refresh when using `IAuthenticationService`:

- Access tokens typically expire after 1 hour
- If `offline_access` scope is granted, MSAL uses the refresh token to obtain new access tokens
- Call `RefreshAsync()` to explicitly refresh tokens:

```csharp
var refreshed = await _authenticationService.RefreshAsync(cancellationToken: ct);

if (refreshed)
{
    // Tokens were refreshed successfully
    var tokens = await _tokenCache.GetAsync(ct);
    var newAccessToken = tokens?["AccessToken"];
}
```

## Part 6: Platform-Specific Configurations

### iOS

**Keychain Configuration:**

iOS stores tokens in the keychain. Configure the security group in `appsettings.json`:

```json
{
  "Msal": {
    "KeychainSecurityGroup": "com.yourcompany.yourapp"
  }
}
```

Or in code:

```csharp
auth.AddMsal(window, msal => msal
    .Builder(builder => builder
        .WithIosKeychainSecurityGroup("com.yourcompany.yourapp")
    )
);
```

**Info.plist Requirements:**

Ensure your `Info.plist` has the necessary entries for opening Safari:

```xml
<key>LSApplicationQueriesSchemes</key>
<array>
    <string>msauthv2</string>
    <string>msauthv3</string>
</array>
```

### Android

**Redirect URI:**

For Android, you can use a custom scheme:

```json
{
  "Msal": {
    "RedirectUri": "msauth://com.yourcompany.yourapp/callback"
  }
}
```

Update Azure AD to include this redirect URI in **Mobile and desktop applications** platform.

**AndroidManifest.xml:**

Add the necessary activity for handling redirects:

```xml
<activity android:name="microsoft.identity.client.BrowserTabActivity">
    <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data
            android:scheme="msauth"
            android:host="com.yourcompany.yourapp"
            android:path="/callback" />
    </intent-filter>
</activity>
```

### WebAssembly

**Browser Considerations:**

MSAL on WebAssembly opens authentication in a popup or redirect:

```json
{
  "Msal": {
    "RedirectUri": "https://yourdomain.com"
  }
}
```

Update Azure AD to include your WebAssembly URL in **Single-page application** platform:

- Add `https://yourdomain.com` or `https://localhost:5000` (for development)

**Pop-up Blockers:**

Ensure users allow pop-ups for authentication dialogs.

### Windows

Windows desktop apps work with the default loopback configuration:

```json
{
  "Msal": {
    "RedirectUri": "http://localhost"
  }
}
```

No additional configuration needed.

## Part 7: Testing Your Configuration

### Verification Checklist

- [ ] Azure AD app registration created
- [ ] Application (client) ID copied
- [ ] Redirect URI configured in Azure AD (Public client)
- [ ] Public client flows enabled
- [ ] API permissions added (including `offline_access`)
- [ ] Admin consent granted (if required)
- [ ] `appsettings.json` created with correct ClientId
- [ ] `appsettings.json` included in build output
- [ ] `AddMsal(window)` called in App.xaml.cs with Window parameter
- [ ] Authentication flow tested on target platform(s)

### Validation with Azure CLI

Run these commands to validate your Azure AD configuration:

```bash
# Check app exists
az ad app show --id <client-id>

# Verify redirect URIs
az ad app show --id <client-id> --query "publicClient.redirectUris"

# Verify tenant configuration
az ad app show --id <client-id> --query "signInAudience"

# List permissions
az ad app permission list --id <client-id>
```

### Common Issues

If you encounter issues, refer to the [MSAL Troubleshooting Guide](xref:Uno.Extensions.Authentication.HowToMsalTroubleshooting).

## Summary

You now have a complete MSAL authentication setup:

1. ✅ Azure AD app registration configured
2. ✅ Uno Platform project configured with AuthenticationMsal
3. ✅ appsettings.json with all necessary configuration
4. ✅ App.xaml.cs with MSAL authentication
5. ✅ Login/logout implementation
6. ✅ Token retrieval for API calls
7. ✅ Platform-specific configurations

## Next Steps

- Explore [MSAL Troubleshooting](xref:Uno.Extensions.Authentication.HowToMsalTroubleshooting) for common issues
- Learn about [Authentication Overview](xref:Uno.Extensions.Authentication.Overview)
- Read [Microsoft Graph API documentation](https://learn.microsoft.com/graph/overview)

## Additional Resources

- [Microsoft identity platform documentation](https://learn.microsoft.com/azure/active-directory/develop/)
- [MSAL.NET documentation](https://learn.microsoft.com/azure/active-directory/develop/msal-overview)
- [Azure CLI reference](https://learn.microsoft.com/cli/azure/ad/app)
- [Uno.Extensions Documentation](https://platform.uno/docs/articles/external/uno.extensions/doc/Overview.html)
