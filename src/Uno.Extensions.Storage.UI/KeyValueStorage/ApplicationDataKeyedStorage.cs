using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Storage;
/*
internal class ApplicationDataKeyedStorage : IKeyedStorage
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
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Clearing value for key '{name}'.");
		}

		var isRemoved = _dataContainer.Values.Remove(GetKey(name));

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Cleared value for key '{name}'.");
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
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Getting value for key '{name}'.");
		}

		if (!_dataContainer.Values.TryGetValue(GetKey(name), out var encryptedData))
		{
			throw new KeyNotFoundException(name);
		}

		var value = await this.DecryptAndDeserialize<T>(ct, (byte[])encryptedData);

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Retrieved value for key '{name}'.");
		}

		return value;
	}

	public event EventHandler<string> ValueChanged;

	/// <inheritdoc />
	public async Task SetValue<T>(CancellationToken ct, string name, T value)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Setting value for key '{name}'.");
		}

		var encryptedData = await this.SerializeAndEncrypt(ct, value);
		_dataContainer.Values[GetKey(name)] = encryptedData;

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Value for key '{name}' set.");
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
