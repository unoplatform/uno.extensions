namespace Uno.Extensions.Storage.KeyValueStorage;

internal record ApplicationDataKeyValueStorage
	(ILogger<ApplicationDataKeyValueStorage> Logger,
	InMemoryKeyValueStorage InMemoryStorage,
	KeyValueStorageSettings Settings,
	ISerializer Serializer,
	ISettings UnpackagedSettings
	) : BaseKeyValueStorageWithCaching(InMemoryStorage, Settings)
{
	public const string Name = "ApplicationData";

	// Do not change this value.
	private const string KeyNameSuffix = "_ADCSSS";

	private ApplicationDataContainer DataContainer => ApplicationData.Current.LocalSettings;

	/// <inheritdoc />
	public override bool IsEncrypted => false;

	private bool UseApplicationData =>
#if !WINDOWS
		true;
#else
		PlatformHelper.IsAppPackaged;
#endif
	private bool TryGetSetting(string name, out object? value)
	{
		if (UseApplicationData)
		{
			return DataContainer.Values.TryGetValue(name, out value);
		}
		else
		{
			return UnpackagedSettings.Get(name) is string t
				? (value = t) is not null
				: (value = null) is not null;
		}
	}
	private void RemoveSetting(string name)
	{
		if (UseApplicationData)
		{
			_ = DataContainer.Values.Remove(name);
		}
		else
		{
			UnpackagedSettings.Remove(name);
		}
	}

	private void SetSetting(string name, object? value)
	{
		if (UseApplicationData)
		{
			DataContainer.Values[name] = value;
		}
		else
		{
			UnpackagedSettings.Set(name, value?.ToString());
		}
	}

	private void ClearSettings()
	{
		if (UseApplicationData)
		{
			DataContainer.Values.Clear();
		}
		else
		{
			UnpackagedSettings.Clear();
		}
	}

	/// <inheritdoc />
	protected override async ValueTask InternalClearAsync(string? name, CancellationToken ct)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Clearing value for key '{name}'.");
		}

		if (name is not null &&
			!string.IsNullOrEmpty(name))
		{
			RemoveSetting(GetKey(name));
		}
		else
		{
			ClearSettings();
		}

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Cleared value for key '{name}'.");
		}

	}

	/// <inheritdoc />
	protected override async ValueTask<string[]> InternalGetKeysAsync(CancellationToken ct)
	{
		if (UseApplicationData)
		{
			return DataContainer
			.Values
			.Keys
			.Where(key => key.EndsWith(KeyNameSuffix))
			.Select(GetName) // filter-out non-encrypted storage
			.Trim()
			.ToArray();
		}
		else
		{
			return UnpackagedSettings
			.Keys
			.Where(key => key.EndsWith(KeyNameSuffix))
			.Select(GetName) // filter-out non-encrypted storage
			.Trim()
			.ToArray();

		}
	}

	/// <inheritdoc />
#nullable disable
	protected override async ValueTask<T> InternalGetAsync<T>(string name, CancellationToken ct)
	{
		try
		{
			if (Logger.IsEnabled(LogLevel.Debug))
			{
				Logger.LogDebugMessage($"Getting value for key '{name}'.");
			}

			if (!TryGetSetting(GetKey(name), out var data))
			{
				throw new KeyNotFoundException(name);
			}

			var value = await GetTypedValue<T>(data, ct);

			if (Logger.IsEnabled(LogLevel.Information))
			{
				Logger.LogInformationMessage($"Retrieved value for key '{name}'.");
			}

			return value;
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Error getting value for key '{name}'.", ex.Message);
			}

			return default;
		}
	}
#nullable restore

	protected virtual async Task<T?> GetTypedValue<T>(object? data, CancellationToken ct)
	{
		return this.Deserialize<T>(data as string);
	}

	protected virtual async Task<object> GetObjectValue<T>(T data, CancellationToken ct) where T : notnull
	{
		return this.Serialize(data);
	}

	/// <inheritdoc />
	protected override async ValueTask InternalSetAsync<T>(string name, T value, CancellationToken ct)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Setting value for key '{name}'.");
		}

		var data = await GetObjectValue(value, ct);
		SetSetting(GetKey(name), data);

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Value for key '{name}' set.");
		}
	}


	private static string GetKey(string name)
	{
		return name + KeyNameSuffix;
	}

	private static string GetName(string key)
	{
		return key.EndsWith(KeyNameSuffix, StringComparison.Ordinal)
			? key.Substring(0, key.Length - KeyNameSuffix.Length)
			: key;
	}

	protected T? Deserialize<T>(string? data)
	{
		if (data is null || string.IsNullOrEmpty(data))
		{
			return default;
		}
		return Serializer.FromString<T>(data);
	}

	protected string Serialize<T>(T value)
	{
		return Serializer.ToString(value);
	}
}
