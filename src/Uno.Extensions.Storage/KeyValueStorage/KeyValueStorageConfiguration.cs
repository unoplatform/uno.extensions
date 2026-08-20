namespace Uno.Extensions.Storage.KeyValueStorage;

internal record KeyValueStorageConfiguration : KeyValueStorageSettings
{
	public IDictionary<string, KeyValueStorageSettings> Providers { get; init; } = new Dictionary<string, KeyValueStorageSettings>();

	/// <summary>
	/// WebAssembly only: which browser store the default <see cref="IKeyValueStorage"/> writes to.
	/// Defaults to <see cref="KeyValueStorage.BrowserCacheLocation.SessionStorage"/>, matching
	/// MSAL.js, so state survives a page reload but not closing the tab.
	/// </summary>
	/// <remarks>
	/// Lives here rather than on a provider's own configuration because it governs the one default
	/// store the whole host shares - the authentication token cache included, whichever provider
	/// populates it. Keying it off an authentication section would mean every app that renames its
	/// provider has to repeat the setting, and an app using a non-MSAL provider would be configuring
	/// its token cache through a section named after MSAL.
	/// </remarks>
	public BrowserCacheLocation BrowserCacheLocation { get; init; } = BrowserCacheLocation.SessionStorage;
}
