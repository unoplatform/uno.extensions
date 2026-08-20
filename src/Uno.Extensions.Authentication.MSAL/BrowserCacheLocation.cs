namespace Uno.Extensions.Authentication.MSAL;

/// <summary>
/// Where the MSAL token cache is kept on WebAssembly. Ignored on every other platform, where the
/// cache is persisted by <c>MsalCacheHelper</c> (DPAPI / Keychain / keyring) or natively by MSAL.
/// </summary>
/// <remarks>
/// The member names mirror msal-browser's own <c>BrowserCacheLocation</c> values
/// (<c>sessionStorage</c> / <c>localStorage</c> / <c>memoryStorage</c>), and the default matches
/// its <c>DEFAULT_CACHE_OPTIONS</c>. Keeping the vocabulary identical means the value maps
/// straight onto msal-browser's <c>cacheLocation</c> if the WebAssembly provider is ever
/// reimplemented on top of it - see specs/011-wasm-msal-token-cache/spec.md.
/// <para>
/// Neither persistent option is protected: browser storage is readable by any script on the
/// origin, and the serialized MSAL cache contains the refresh token, not just the access token.
/// What bounds the exposure is the refresh token's lifetime - 24 hours, non-sliding, for a
/// redirect URI registered under the Entra <c>spa</c> platform.
/// </para>
/// </remarks>
internal enum BrowserCacheLocation
{
	/// <summary>
	/// Persist to <c>sessionStorage</c>: the cache survives a page reload but is dropped when the
	/// tab closes. The default, matching MSAL.js.
	/// </summary>
	SessionStorage,

	/// <summary>
	/// Persist to <c>localStorage</c>: the cache also survives closing the tab and restarting the
	/// browser, which widens the window in which a stolen refresh token remains usable. Opt in
	/// deliberately.
	/// </summary>
	LocalStorage,

	/// <summary>
	/// Never write the cache to browser storage; it lives for the lifetime of the page only, so
	/// the user signs in again after every reload.
	/// </summary>
	MemoryStorage,
}
