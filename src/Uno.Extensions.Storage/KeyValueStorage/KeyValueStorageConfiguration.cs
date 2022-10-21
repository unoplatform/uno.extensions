namespace Uno.Extensions.Storage.KeyValueStorage;

public record KeyValueStorageConfiguration
{
	public IDictionary<string, KeyValueStorageSettings> StorageProviders { get; init; } = new Dictionary<string, KeyValueStorageSettings>();


}

internal static class KeyValueStorageConfigurationExtensions
{
	public static KeyValueStorageSettings GetSettingsOrDefault(this KeyValueStorageConfiguration? config, string name)
	{
		if (config?.StorageProviders.TryGetValue(name, out var settings) ?? false)
		{
			return settings;
		}
		return new KeyValueStorageSettings();
	}
}

public record KeyValueStorageSettings
{
	public bool DisableInMemoryCache { get; init; }
}
