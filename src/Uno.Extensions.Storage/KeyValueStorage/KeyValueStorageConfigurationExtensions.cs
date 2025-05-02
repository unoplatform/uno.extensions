namespace Uno.Extensions.Storage.KeyValueStorage;

internal static class KeyValueStorageConfigurationExtensions
{
	/// <summary>
	/// Retrieves the <see cref="KeyValueStorageSettings"/> associated with the specified name
	/// from the given <see cref="KeyValueStorageConfiguration"/>. If no settings are found
	/// for the specified name, the default settings are returned.
	/// </summary>
	/// <param name="config">
	/// The <see cref="KeyValueStorageConfiguration"/> instance to retrieve the settings from.
	/// This parameter can be null.
	/// </param>
	/// <param name="name">
	/// The name of the settings to retrieve.
	/// </param>
	/// <returns>
	/// The <see cref="KeyValueStorageSettings"/> associated with the specified name, or
	/// the default settings if no match is found.
	/// </returns>

	public static KeyValueStorageSettings GetSettingsOrDefault(this KeyValueStorageConfiguration? config, string name)
	{
		if (config?.Providers.TryGetValue(name, out var settings) ?? false)
		{
			return settings;
		}

		// If there isn't a match for settings for the supplied name, return
		// the default settings
		return config ?? new KeyValueStorageSettings();
	}
}
