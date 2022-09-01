

using Windows.Security.Cryptography.DataProtection;

namespace Uno.Extensions.Storage.KeyValueStorage;

internal record ApplicationDataKeyValueStorage(ILogger<ApplicationDataKeyValueStorage> Logger, ISerializer Serializer) : IKeyValueStorage
{
	public const string Name = "ApplicationData";

	// Do not change this value.
	private const string KeyNameSuffix = "_ADCSSS";

	private readonly ApplicationDataContainer _dataContainer = ApplicationData.Current.LocalSettings;

	/// <inheritdoc />
	public async ValueTask ClearAsync(string? name, CancellationToken ct)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Clearing value for key '{name}'.");
		}

		if (name is not null &&
			!string.IsNullOrEmpty(name))
		{
			_ = _dataContainer.Values.Remove(GetKey(name));

		}
		else
		{
			_dataContainer.Values.Clear();

		}

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Cleared value for key '{name}'.");
		}

	}

	/// <inheritdoc />
	public async ValueTask<string[]> GetKeysAsync(CancellationToken ct)
	{
		return _dataContainer
			.Values
			.Keys
			.Select(key => GetName(key)) // filter-out non-encrypted storage
			.Trim()
			.ToArray();
	}

	/// <inheritdoc />
	public async ValueTask<T?> GetAsync<T>(string name, CancellationToken ct)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Getting value for key '{name}'.");
		}

		if (!_dataContainer.Values.TryGetValue(GetKey(name), out var data))
		{
			throw new KeyNotFoundException(name);
		}

		var value = await GetTypedValue<T>(data,ct);

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Retrieved value for key '{name}'.");
		}

		return value;
	}

	protected virtual async ValueTask<T?> GetTypedValue<T>(object? data, CancellationToken ct)
	{
		return this.Deserialize<T>(data as string);
	}

	protected virtual async ValueTask<object> GetObjectValue<T>(T data, CancellationToken ct) where T :notnull
	{
		return this.Serialize(data);
	}

	/// <inheritdoc />
	public async ValueTask SetAsync<T>(string name, T value, CancellationToken ct) where T : notnull
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Setting value for key '{name}'.");
		}

		var data= await GetObjectValue(value, ct);
		_dataContainer.Values[GetKey(name)] = data;

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

/*
 * 
 * using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Storage;

internal class ApplicationDataKeyedStorage : IKeyValueStorage
{
	public const string Name = "ApplicationData";

	// Do not change this value.
	private const string KeyNameSuffix = "_ADCSSS";

	private readonly ISerializer _serializer;
	private readonly ApplicationDataContainer _dataContainer;
	private readonly DataProtectionProvider _provider;

	private const string DataProtectionProviderDescriptor = "LOCAL=user";

	/// <summary>
	/// Creates a new <see cref="ApplicationDataContainerSecureSettingsStorage"/> with a specific <see cref="ApplicationDataContainer"/>
	/// to save data into.
	/// </summary>
	/// <param name="serializer">A serializer for transforming values back and forth to strings.</param>
	/// <param name="dataContainer">The container to store data into and retrive data from.</param>
	public ApplicationDataContainerSecureSettingsStorage(
		ISettingsSerializer serializer,
		ApplicationDataContainer dataContainer)
	{
		_serializer = serializer;
		_dataContainer = dataContainer;

		_provider = new DataProtectionProvider(DataProtectionProviderDescriptor);
	}

	/// <inheritdoc />
	public async Task ClearValue(CancellationToken ct, string name)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Clearing value for key '{name}'.");
		}

		var isRemoved = _dataContainer.Values.Remove(GetKey(name));

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Cleared value for key '{name}'.");
		}

		if (isRemoved)
		{
			ValueChanged?.Invoke(this, name);
		}
	}

	/// <inheritdoc />
	public async Task<string[]> GetAllKeys(CancellationToken ct)
	{
		return _dataContainer
			.Values
			.Keys
			.Select(key => GetName(key)) // filter-out non-encrypted storage
			.Trim()
			.ToArray();
	}

	/// <inheritdoc />
	public async Task<T> GetValue<T>(CancellationToken ct, string name)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Getting value for key '{name}'.");
		}

		if (!_dataContainer.Values.TryGetValue(GetKey(name), out var encryptedData))
		{
			throw new KeyNotFoundException(name);
		}

		var value = await this.DecryptAndDeserialize<T>(ct, (byte[])encryptedData);

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Retrieved value for key '{name}'.");
		}

		return value;
	}

	public event EventHandler<string> ValueChanged;

	/// <inheritdoc />
	public async Task SetValue<T>(CancellationToken ct, string name, T value)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Setting value for key '{name}'.");
		}

		var encryptedData = await this.SerializeAndEncrypt(ct, value);
		_dataContainer.Values[GetKey(name)] = encryptedData;

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Value for key '{name}' set.");
		}

		ValueChanged?.Invoke(this, name);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		ValueChanged = null;
	}

	private static string GetKey(string name)
	{
		return name + KeyNameSuffix;
	}

	private static string GetName(string key)
	{
		return key.EndsWith(KeyNameSuffix, StringComparison.Ordinal)
			? key.Substring(0, key.Length - KeyNameSuffix.Length)
			: null;
	}

	private async Task<T> DecryptAndDeserialize<T>(CancellationToken ct, byte[] encryptedData)
	{
		var encryptedBuffer = CryptographicBuffer.CreateFromByteArray(encryptedData);
		var decryptedBuffer = await _provider.UnprotectAsync(encryptedBuffer).AsTask(ct);
		var data = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decryptedBuffer);

		return (T)_serializer.FromString(data, typeof(T));
	}

	private async Task<byte[]> SerializeAndEncrypt<T>(CancellationToken ct, T value)
	{
		var data = _serializer.ToString(value, typeof(T));
		var decryptedBuffer = CryptographicBuffer.ConvertStringToBinary(data, BinaryStringEncoding.Utf8);
		var encryptedBuffer = await _provider.ProtectAsync(decryptedBuffer).AsTask(ct);

		CryptographicBuffer.CopyToByteArray(encryptedBuffer, out var encryptedData);

		return encryptedData;
	}
}

*/
