// Compiled out on WebAssembly (UNO_EXT_MSAL_BROWSER), where the cache goes through
// MsalTokenCacheStore rather than MsalCacheHelper. Keep this file free of other #if branches and of
// dependencies beyond Microsoft.Identity.Client.Extensions.Msal: Uno.Extensions.Authentication.MSAL.Tests
// compiles it as linked source (the WinUI assembly can't load in a plain test host).
#if !UNO_EXT_MSAL_BROWSER
namespace Uno.Extensions.Authentication.MSAL;

/// <summary>
/// Computes the default token-cache storage properties used by MsalAuthenticationProvider
/// when the app doesn't supply its own via <see cref="MsalAuthenticationBuilderExtensions.Storage"/>.
/// </summary>
internal static class MsalStorageDefaults
{
	// Keychain/keyring entries are keyed on these values; the ClientId suffix keeps caches of
	// different app registrations apart while letting apps that share a registration share sign-in state.
	// They are also a persisted-storage contract: renaming them orphans existing user caches.
	internal const string ServiceNamePrefix = "uno.extensions.msal";
	internal const string DefaultMacKeychainAccountName = "MSALCache";
	internal const string LinuxKeyringSchemaName = "com.unoplatform.extensions.tokencache";
	internal const string LinuxKeyringSecretLabel = "MSAL token cache for Uno Platform applications";
	internal const string LinuxKeyringClientIdAttributeKey = "MsalClientID";
	internal const string LinuxKeyringProductAttributeKey = "Product";
	internal const string LinuxKeyringProductAttributeValue = "Uno.Extensions";

	internal static string GetMacKeychainServiceName(string? configuredServiceName, string? clientId) =>
		configuredServiceName is { Length: > 0 }
			? configuredServiceName
			: clientId is { Length: > 0 }
				? $"{ServiceNamePrefix}.{clientId}"
				: ServiceNamePrefix;

	internal static string GetMacKeychainAccountName(string? configuredAccountName) =>
		configuredAccountName is { Length: > 0 }
			? configuredAccountName
			: DefaultMacKeychainAccountName;

	/// <summary>
	/// Applies the OS-specific secure-storage defaults that <see cref="MsalCacheHelper"/> requires
	/// on macOS (keychain) and Linux (keyring). Without these the cache helper throws
	/// <see cref="ArgumentNullException"/> on those platforms (https://github.com/unoplatform/uno.extensions/issues/3025).
	/// </summary>
	internal static void ApplyDefaults(
		StorageCreationPropertiesBuilder builder,
		string? clientId,
		string? configuredServiceName,
		string? configuredAccountName,
		bool isMacOS,
		bool isLinux)
	{
		if (isMacOS)
		{
			builder.WithMacKeyChain(
				GetMacKeychainServiceName(configuredServiceName, clientId),
				GetMacKeychainAccountName(configuredAccountName));
		}

		if (isLinux)
		{
			builder.WithLinuxKeyring(
				LinuxKeyringSchemaName,
				MsalCacheHelper.LinuxKeyRingDefaultCollection,
				LinuxKeyringSecretLabel,
				new KeyValuePair<string, string>(LinuxKeyringClientIdAttributeKey, clientId ?? string.Empty),
				new KeyValuePair<string, string>(LinuxKeyringProductAttributeKey, LinuxKeyringProductAttributeValue));
		}
	}
}
#endif
