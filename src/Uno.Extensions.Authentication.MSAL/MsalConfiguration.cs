namespace Uno.Extensions.Authentication.MSAL;

internal class MsalConfiguration
#if UNO_EXT_MSAL
	: PublicClientApplicationOptions
#endif
{
	public string[]? Scopes { get; init; }

	/// <summary>
	/// macOS only: the keychain service name used to store the MSAL token cache.
	/// Defaults to "uno.extensions.msal.{ClientId}" when not specified.
	/// </summary>
	public string? KeychainServiceName { get; init; }

	/// <summary>
	/// macOS only: the keychain account name used to store the MSAL token cache.
	/// Defaults to "MSALCache" when not specified.
	/// </summary>
	public string? KeychainAccountName { get; init; }

	/// <summary>
	/// When true (the default), the provider applies the platform's conventional redirect URI when
	/// none was supplied: <c>msal{ClientId}://auth</c> on Android, <c>msauth.{BundleId}://auth</c>
	/// on iOS, the WebAuthenticationBroker callback on WebAssembly, and MSAL's own
	/// <c>WithDefaultRedirectUri()</c> (<c>http://localhost</c>) elsewhere.
	/// </summary>
	/// <remarks>
	/// A <c>RedirectUri</c> in configuration, or one set from the app's <c>Builder(...)</c>
	/// callback, always wins over the default. Set this to <c>false</c> to suppress the default
	/// entirely and take whatever MSAL itself would use.
	/// </remarks>
	public bool UseDefaultPlatformRedirectUri { get; init; } = true;

	/// <summary>
	/// Maximum time an interactive sign-in may take before it is treated as abandoned and
	/// cancelled. Defaults to 5 minutes. Set to <see cref="TimeSpan.Zero"/> or a negative value
	/// to wait indefinitely.
	/// </summary>
	/// <remarks>
	/// The system-browser flow used on desktop has no way to detect the browser window being
	/// closed, so without this timeout an abandoned sign-in never completes and the awaiting
	/// command stays busy forever.
	/// </remarks>
	public TimeSpan? InteractiveTimeout { get; init; }

	// Where the token cache lives on WebAssembly is configured as
	// KeyValueStorageConfiguration:BrowserCacheLocation, not here: it selects the host's single
	// default IKeyValueStorage, which the Uno token cache shares with every other provider. See
	// specs/011-wasm-msal-token-cache/spec.md.

	/// <summary>
	/// When true, the token cache falls back to an unprotected (plaintext) cache file if the
	/// platform's secure storage (keychain / keyring / DPAPI) isn't available, so sign-in state
	/// still survives an app restart. When false (the default), the cache stays in memory for
	/// the session instead of being written unprotected to disk.
	/// </summary>
	public bool AllowUnprotectedTokenCacheFallback { get; init; }

	/// <summary>
	/// Desktop only: when the provider runs MsalCacheHelper's persistence self-check. Defaults to
	/// <see cref="MsalCachePersistenceCheck.Auto"/>, which checks only the first time a cache is
	/// persisted at a given location.
	/// </summary>
	/// <remarks>
	/// Mostly a macOS concern. The check probes the keychain with an entry whose service name MSAL
	/// randomizes on every run, so under <see cref="MsalCachePersistenceCheck.Always"/> a user is
	/// asked to grant keychain access on every single launch and can never make the prompt stick.
	/// </remarks>
	public MsalCachePersistenceCheck VerifyCachePersistence { get; init; } = MsalCachePersistenceCheck.Auto;
}
