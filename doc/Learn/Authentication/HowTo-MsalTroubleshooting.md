---
uid: Uno.Extensions.Authentication.HowToMsalTroubleshooting
---
# Troubleshooting MSAL Authentication

> **UnoFeatures:** `AuthenticationMsal` (add to `<UnoFeatures>` in your `.csproj`)

This guide provides systematic troubleshooting methodology for MSAL authentication issues in Uno Platform applications.

## General Debugging Approach

### 1. Verify Configuration Extraction

First, ensure your configuration is being loaded correctly from `appsettings.json`:

```csharp
// Add to your App.xaml.cs or startup code temporarily for debugging
var config = host.Services.GetRequiredService<IConfiguration>();
var clientId = config["Msal:ClientId"];
var scopes = config.GetSection("Msal:Scopes").Get<string[]>();
var redirectUri = config["Msal:RedirectUri"];
var authority = config["Msal:Authority"];

Console.WriteLine($"ClientId: {clientId}");
Console.WriteLine($"Scopes: {string.Join(", ", scopes ?? Array.Empty<string>())}");
Console.WriteLine($"RedirectUri: {redirectUri}");
Console.WriteLine($"Authority: {authority}");
```

### 2. Configuration File Precedence

Uno.Extensions uses .NET configuration precedence:

1. `appsettings.development.json` (Development environment)
2. `appsettings.json` (Base configuration)
3. Environment variables
4. Command-line arguments

**Tip:** Check which file is being loaded by examining build output or using the configuration extraction code above.

### 3. Systematic Diagnosis Checklist

When encountering MSAL authentication issues, follow this checklist:

- [ ] Verify ClientId matches Azure AD app registration
- [ ] Confirm redirect URI is configured in both appsettings.json and Azure AD
- [ ] Check that redirect URI is a loopback address (`http://localhost` or `http://localhost:port`)
- [ ] Validate Authority/Tenant configuration matches Azure AD settings
- [ ] Ensure required API permissions are granted in Azure AD
- [ ] Verify platform-specific configurations (iOS keychain, Android, etc.)
- [ ] Check that the Window instance is passed to `AddMsal(window)`

## Validating Azure AD Configuration

### Using Azure CLI

Install Azure CLI and authenticate:

```bash
az login
```

Verify your app registration configuration:

```bash
# Replace <client-id> with your actual ClientId
az ad app show --id <client-id> --query "{signInAudience:signInAudience, publicClient:publicClient.redirectUris, web:web.redirectUris, replyUrls:replyUrlsWithType}"
```

Example output:

```json
{
  "publicClient": {
    "redirectUris": [
      "http://localhost",
      "http://localhost:5000"
    ]
  },
  "signInAudience": "AzureADMyOrg",
  "web": {
    "redirectUris": []
  }
}
```

**Key points:**
- Redirect URIs should be in `publicClient.redirectUris`, not `web.redirectUris`
- For mobile/desktop apps, use public client flow
- `signInAudience` determines tenant configuration

### Using Microsoft.Graph PowerShell

Install and connect to Microsoft Graph:

```powershell
Install-Module Microsoft.Graph -Scope CurrentUser
Connect-MgGraph -Scopes "Application.Read.All"
```

Verify app registration:

```powershell
$clientId = "your-client-id"
$app = Get-MgApplication -Filter "appId eq '$clientId'"
$app | Select-Object AppId, DisplayName, SignInAudience
$app.PublicClient
$app.Web
```

## Common Issues and Solutions

### Issue 1: MsalClientException - Loopback Redirect URI

**Error:**
```
Microsoft.Identity.Client.MsalClientException: Only loopback redirect uri is supported, but <your_redirect_uri> was found.
```

**Cause:** The redirect URI is not a loopback address, or the Window instance was not passed to `AddMsal()`.

**Solution:**

1. Ensure Window is passed to AddMsal:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure((host, window) =>  // Use this overload
        {
            host.UseAuthentication(auth =>
            {
                auth.AddMsal(window);  // Pass window here
            });
        });
}
```

2. Update `appsettings.json` to use loopback URI:

```json
{
  "Msal": {
    "ClientId": "your-client-id",
    "RedirectUri": "http://localhost",
    "Scopes": ["User.Read"]
  }
}
```

3. Update Azure AD app registration:
   - Go to **Authentication** > **Platform configurations** > **Mobile and desktop applications**
   - Add `http://localhost` as a redirect URI
   - Enable public client flows

**Validation:**

```bash
az ad app show --id <client-id> --query "publicClient.redirectUris"
```

Should return: `["http://localhost"]`

### Issue 2: AADSTS700016 - Application Not Found

**Error:**
```
AADSTS700016: Application with identifier '<client-id>' was not found in the directory '<tenant-id>'.
```

**Cause:** ClientId doesn't match any app registration in the specified tenant.

**Solution:**

1. Verify ClientId in appsettings.json:

```bash
# Extract from appsettings
cat appsettings.json | grep ClientId
```

2. Verify app exists in Azure AD:

```bash
az ad app show --id <client-id>
```

If the command fails, the app doesn't exist or you don't have permission.

3. Check if you're using the correct tenant:
   - For single tenant: `"Authority": "https://login.microsoftonline.com/<tenant-id>"`
   - For multi-tenant: `"Authority": "https://login.microsoftonline.com/common"`
   - For work/school accounts: `"Authority": "https://login.microsoftonline.com/organizations"`

### Issue 3: AADSTS50011 - Reply URL Mismatch

**Error:**
```
AADSTS50011: The reply URL specified in the request does not match the reply URLs configured for the application.
```

**Cause:** The redirect URI in your app doesn't match what's registered in Azure AD.

**Solution:**

1. Check your configured redirect URI:

```bash
# In your app
cat appsettings.json | grep RedirectUri

# In Azure AD
az ad app show --id <client-id> --query "publicClient.redirectUris"
```

2. They must match exactly. Update Azure AD:

```bash
# Add redirect URI (if missing)
az ad app update --id <client-id> --public-client-redirect-uris "http://localhost"
```

3. For apps that specify a port, ensure consistency:

```json
{
  "Msal": {
    "RedirectUri": "http://localhost:5000"
  }
}
```

And in Azure AD, add `http://localhost:5000` as well.

### Issue 4: AADSTS65001 - User Consent Required

**Error:**
```
AADSTS65001: The user or administrator has not consented to use the application.
```

**Cause:** Required API permissions haven't been granted admin consent.

**Solution:**

1. Check required permissions in Azure AD:
   - Portal: **API permissions** section
   - CLI: `az ad app permission list --id <client-id>`

2. Grant admin consent:
   - Portal: Click **Grant admin consent for \<tenant\>** button
   - CLI:
     ```bash
     # List service principals
     az ad sp list --filter "appId eq '<client-id>'"
     
     # Grant admin consent (requires admin role)
     az ad app permission admin-consent --id <client-id>
     ```

3. For delegated permissions (like User.Read), consent may be per-user. Ensure your scopes match:

```json
{
  "Msal": {
    "Scopes": ["User.Read"]  // Must match API permissions
  }
}
```

### Issue 5: AADSTS90002 - Tenant Not Found

**Error:**
```
AADSTS90002: Tenant '<tenant-id>' not found.
```

**Cause:** The tenant ID in the Authority URL doesn't exist or is incorrect.

**Solution:**

1. Find your tenant ID:

```bash
az account show --query tenantId
```

2. Update Authority in appsettings.json:

```json
{
  "Msal": {
    "Authority": "https://login.microsoftonline.com/<correct-tenant-id>"
  }
}
```

3. For multi-tenant apps, use `common` or `organizations`:

```json
{
  "Msal": {
    "Authority": "https://login.microsoftonline.com/common"
  }
}
```

### Issue 6: No ClientId Specified

**Error:**
```
Microsoft.Identity.Client.MsalClientException: No ClientId was specified.
```

**Cause:** ClientId is not found in configuration.

**Solution:**

1. Verify appsettings.json exists and is included in build:
   - Check `.csproj` for `<Content Include="appsettings*.json" />`

2. Ensure configuration section name matches:

```json
{
  "Msal": {  // Section name must be "Msal" (default)
    "ClientId": "your-guid-here"
  }
}
```

3. If using a custom section name, configure it:

```csharp
auth.AddMsal(window, config: builder.Configuration.GetSection("MyCustomMsal"))
```

### Issue 7: Token Expiration / Refresh Issues

**Symptoms:** User is logged out unexpectedly, or errors occur after a period of inactivity.

**Solution:**

1. MSAL automatically handles token refresh. Verify refresh tokens are enabled:
   - Azure AD: **API permissions** should include `offline_access` scope

2. Add offline_access to scopes:

```json
{
  "Msal": {
    "Scopes": ["User.Read", "offline_access"]
  }
}
```

3. Check token cache configuration (platform-specific):

```csharp
// iOS - configure keychain access group if needed
auth.AddMsal(window, msal => msal
    .Builder(builder => builder
        .WithIosKeychainSecurityGroup("com.yourcompany.yourapp")
    )
);
```

## Multi-Tenant Configuration

### Single Tenant (Default)

For apps that only sign in users from one Azure AD tenant:

```json
{
  "Msal": {
    "ClientId": "your-client-id",
    "Authority": "https://login.microsoftonline.com/<tenant-id>",
    "Scopes": ["User.Read"]
  }
}
```

**Azure AD Setting:** `signInAudience: "AzureADMyOrg"`

**Validation:**
```bash
az ad app show --id <client-id> --query "signInAudience"
```

### Multi-Tenant (Any Organization)

For apps that sign in users from any Azure AD organization:

```json
{
  "Msal": {
    "ClientId": "your-client-id",
    "Authority": "https://login.microsoftonline.com/organizations",
    "Scopes": ["User.Read"]
  }
}
```

**Azure AD Setting:** `signInAudience: "AzureADMultipleOrgs"`

### Multi-Tenant (Any Microsoft Account)

For apps that sign in users with work/school or personal Microsoft accounts:

```json
{
  "Msal": {
    "ClientId": "your-client-id",
    "Authority": "https://login.microsoftonline.com/common",
    "Scopes": ["User.Read"]
  }
}
```

**Azure AD Setting:** `signInAudience: "AzureADandPersonalMicrosoftAccount"`

## Platform-Specific Issues

### iOS

**Keychain Access:**

If you encounter keychain errors, configure the security group:

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

### Android

**Redirect URI Format:**

Android may require a specific redirect URI format for system browser:

```json
{
  "Msal": {
    "RedirectUri": "msauth://com.yourcompany.yourapp/callback"
  }
}
```

Update Azure AD accordingly.

### WebAssembly

**Browser Limitations:**

MSAL on WebAssembly uses the system browser. Ensure:
- Redirect URI is registered in Azure AD
- Pop-up blockers are not preventing authentication
- CORS is configured if calling APIs

## Diagnostic Tools

### Enable MSAL Logging

Add logging to see detailed MSAL diagnostics:

```csharp
auth.AddMsal(window, msal => msal
    .Builder(builder => builder
        .WithLogging((level, message, containsPii) =>
        {
            Console.WriteLine($"[MSAL {level}] {message}");
        }, LogLevel.Verbose, enablePiiLogging: false, enableDefaultPlatformLogging: true)
    )
);
```

**Warning:** Never enable PII logging in production.

### Test Configuration Endpoint

Verify Microsoft identity platform is reachable:

```bash
# Test authority endpoint
curl https://login.microsoftonline.com/<tenant-id>/v2.0/.well-known/openid-configuration
```

Should return OpenID configuration JSON.

## Quick Reference: Configuration Validation Commands

```bash
# Verify app registration exists
az ad app show --id <client-id>

# Check redirect URIs
az ad app show --id <client-id> --query "publicClient.redirectUris"

# Check sign-in audience (tenant configuration)
az ad app show --id <client-id> --query "signInAudience"

# List API permissions
az ad app permission list --id <client-id>

# Check service principal (if app is registered)
az ad sp show --id <client-id>
```

## For AI Assistants

When helping users debug MSAL authentication:

1. **Extract configuration first:** Ask for or help extract the configuration from appsettings.json
2. **Validate with Azure CLI/PowerShell:** Use the commands above to verify Azure AD configuration directly
3. **Map errors to solutions:** Use the AADSTS error code to find the relevant section above
4. **Provide actionable fixes:** Give exact commands to run, not just descriptions
5. **Check platform specifics:** Ask what platform(s) they're targeting (iOS, Android, WebAssembly, etc.)

## Additional Resources

- [Microsoft identity platform documentation](https://learn.microsoft.com/azure/active-directory/develop/)
- [MSAL.NET documentation](https://learn.microsoft.com/azure/active-directory/develop/msal-overview)
- [Azure AD error codes](https://learn.microsoft.com/azure/active-directory/develop/reference-aadsts-error-codes)
- [Uno.Extensions Authentication Overview](xref:Uno.Extensions.Authentication.Overview)
- [How-To: Use MSAL Authentication](xref:Uno.Extensions.Authentication.HowToMsalAuthentication)
