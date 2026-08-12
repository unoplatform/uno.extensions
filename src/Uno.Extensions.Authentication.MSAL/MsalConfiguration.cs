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
	/// When true, the token cache falls back to an unprotected (plaintext) cache file if the
	/// platform's secure storage (keychain / keyring / DPAPI) isn't available, so sign-in state
	/// still survives an app restart. When false (the default), the cache stays in memory for
	/// the session instead of being written unprotected to disk.
	/// </summary>
	public bool AllowUnprotectedTokenCacheFallback { get; init; }
}
