// Compiled out on WebAssembly (UNO_EXT_MSAL_BROWSER), where the cache goes through
// MsalTokenCacheStore rather than MsalCacheHelper. Keep this file free of other #if branches and of
// dependencies beyond Microsoft.Identity.Client.Extensions.Msal: Uno.Extensions.Authentication.MSAL.Tests
// compiles it as linked source (the WinUI assembly can't load in a plain test host).
#if !UNO_EXT_MSAL_BROWSER
namespace Uno.Extensions.Authentication.MSAL;

/// <summary>
/// The OS secure store that <see cref="MsalCacheHelper"/> must be told about before it can
/// persist the token cache on desktop. One value per platform: the two are never both true.
/// </summary>
internal enum MsalSecureStore
{
	/// <summary>Windows (DPAPI file protection) and everything else: no extra properties.</summary>
	None,
	/// <summary>The macOS keychain.</summary>
	MacKeychain,
	/// <summary>The Linux keyring (libsecret).</summary>
	LinuxKeyring,
}

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
		MsalSecureStore store)
	{
		switch (store)
		{
			case MsalSecureStore.MacKeychain:
				builder.WithMacKeyChain(
					GetMacKeychainServiceName(configuredServiceName, clientId),
					GetMacKeychainAccountName(configuredAccountName));
				break;
			case MsalSecureStore.LinuxKeyring:
				builder.WithLinuxKeyring(
					LinuxKeyringSchemaName,
					MsalCacheHelper.LinuxKeyRingDefaultCollection,
					LinuxKeyringSecretLabel,
					new KeyValuePair<string, string>(LinuxKeyringClientIdAttributeKey, clientId ?? string.Empty),
					new KeyValuePair<string, string>(LinuxKeyringProductAttributeKey, LinuxKeyringProductAttributeValue));
				break;
		}
	}

	/// <summary>
	/// The store <see cref="MsalCacheHelper"/> needs configured on the current desktop OS.
	/// </summary>
	internal static MsalSecureStore ForCurrentOS() =>
		OperatingSystem.IsMacOS() ? MsalSecureStore.MacKeychain
		: OperatingSystem.IsLinux() ? MsalSecureStore.LinuxKeyring
		: MsalSecureStore.None;

	/// <summary>
	/// Whether <c>MsalCacheHelper.VerifyPersistence()</c> should run for a cache whose secure store
	/// has already accepted a write (<paramref name="cacheAlreadyPersisted"/>).
	/// </summary>
	/// <remarks>
	/// Under <see cref="MsalCachePersistenceCheck.Auto"/> the check runs only when nothing has been
	/// persisted at this location yet. The caller establishes that from the cache file: every
	/// <c>ICacheAccessor</c> touches it when it writes (<c>FileIOWithRetries.TouchFile</c>) and
	/// deletes it in <c>Clear</c>, so the file existing means a write already succeeded with this
	/// configuration - on macOS too, where the payload itself lives in the keychain rather than in
	/// that file.
	/// </remarks>
	internal static bool ShouldVerifyPersistence(MsalCachePersistenceCheck mode, bool cacheAlreadyPersisted) =>
		mode switch
		{
			MsalCachePersistenceCheck.Always => true,
			MsalCachePersistenceCheck.Never => false,
			_ => !cacheAlreadyPersisted,
		};
}
#endif
