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
/// Configured as <c>KeyValueStorageConfiguration:BrowserCacheLocation</c>. When absent the default
/// is <see cref="LocalStorage"/> - the store WebAssembly already used before this setting existed,
/// so upgrading a package never relocates an app's data. That is a deliberate deviation from
/// msal-browser, whose <c>DEFAULT_CACHE_OPTIONS</c> default is <c>sessionStorage</c>: an app that
/// wants the tighter session-scoped lifetime opts in, rather than every existing app silently
/// losing what it had persisted. See specs/011-wasm-msal-token-cache/spec.md.
/// </para>
/// <para>
/// The member names still mirror msal-browser's own <c>BrowserCacheLocation</c> values
/// (<c>sessionStorage</c> / <c>localStorage</c> / <c>memoryStorage</c>): sharing that vocabulary
/// means the value maps straight onto msal-browser's <c>cacheLocation</c> if the WebAssembly
/// provider is ever reimplemented on top of it.
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
	/// closes. The tightest lifetime of the two persistent options, and what MSAL.js defaults to -
	/// worth opting into for anything holding credentials.
	/// </summary>
	SessionStorage,

	/// <summary>
	/// Persist to <c>localStorage</c>: entries also survive closing the tab and restarting the
	/// browser, which widens the window in which a stolen token remains usable. The default, because
	/// it is what WebAssembly already used before this setting existed.
	/// </summary>
	LocalStorage,

	/// <summary>
	/// Never write to browser storage; values live for the lifetime of the page only, so the user
	/// signs in again after every reload.
	/// </summary>
	MemoryStorage,
}
