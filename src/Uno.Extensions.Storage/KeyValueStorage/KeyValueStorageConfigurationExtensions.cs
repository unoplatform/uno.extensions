﻿namespace Uno.Extensions.Storage.KeyValueStorage;

internal static class KeyValueStorageConfigurationExtensions
{
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
