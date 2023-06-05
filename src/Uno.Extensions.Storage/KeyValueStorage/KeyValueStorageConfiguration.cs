namespace Uno.Extensions.Storage.KeyValueStorage;

internal record KeyValueStorageConfiguration : KeyValueStorageSettings
{
	public IDictionary<string, KeyValueStorageSettings> Providers { get; init; } = new Dictionary<string, KeyValueStorageSettings>();
}
