namespace Uno.Extensions.Storage.KeyValueStorage;

/// <summary>
/// Where key-value storage is kept on WebAssembly. Ignored on every other platform, which each have
/// their own store (DPAPI, Keychain, KeyStore, or a file).
/// </summary>
/// <remarks>
/// This governs the default <see cref="IKeyValueStorage"/>, so it applies to everything built on it
/// - most visibly the authentication token cache, whichever provider populates it, and the
/// serialized MSAL cache on the WebAssembly head.
/// <para>
/// The member names mirror msal-browser's own <c>BrowserCacheLocation</c> values
/// (<c>sessionStorage</c> / <c>localStorage</c> / <c>memoryStorage</c>), and the default matches its
/// <c>DEFAULT_CACHE_OPTIONS</c>: sharing that vocabulary means the value maps straight onto
/// msal-browser's <c>cacheLocation</c> if the WebAssembly provider is ever reimplemented on top of
/// it - see specs/011-wasm-msal-token-cache/spec.md.
/// </para>
/// <para>
/// Neither persistent option is protected: browser storage is readable by any script on the origin,
/// and a serialized MSAL cache contains the refresh token, not just the access token. What bounds
/// the exposure is the refresh token's lifetime - 24 hours, non-sliding, for a redirect URI
/// registered under the Entra <c>spa</c> platform.
/// </para>
/// </remarks>
internal enum BrowserCacheLocation
{
	/// <summary>
	/// Persist to <c>sessionStorage</c>: entries survive a page reload but are dropped when the tab
	/// closes. The default, matching MSAL.js.
	/// </summary>
	SessionStorage,

	/// <summary>
	/// Persist to <c>localStorage</c>: entries also survive closing the tab and restarting the
	/// browser, which widens the window in which a stolen token remains usable. Opt in deliberately.
	/// </summary>
	LocalStorage,

	/// <summary>
	/// Never write to browser storage; values live for the lifetime of the page only, so the user
	/// signs in again after every reload.
	/// </summary>
	MemoryStorage,
}
