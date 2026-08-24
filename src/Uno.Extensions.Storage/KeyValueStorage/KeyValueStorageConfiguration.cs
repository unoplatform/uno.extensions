namespace Uno.Extensions.Storage.KeyValueStorage;

internal record KeyValueStorageConfiguration : KeyValueStorageSettings
{
	public IDictionary<string, KeyValueStorageSettings> Providers { get; init; } = new Dictionary<string, KeyValueStorageSettings>();

	// This section also carries "BrowserCacheLocation" (see the BrowserCacheLocation enum), which is
	// deliberately NOT a property here: it selects a *named* IKeyValueStorage registration, so the
	// only thing that can act on it is AddKeyedStorage at registration time - before options bind.
	// Binding it here as well gave one key two readers with two answers for a bad value.
}
