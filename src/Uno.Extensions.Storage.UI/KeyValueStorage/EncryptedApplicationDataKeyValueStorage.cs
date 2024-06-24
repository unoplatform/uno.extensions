#if WINDOWS || WINDOWS_UWP

using Uno.Extensions.Serialization;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;

namespace Uno.Extensions.Storage.KeyValueStorage;

internal record EncryptedApplicationDataKeyValueStorage(
	ILogger<ApplicationDataKeyValueStorage> EncryptedLogger,
	InMemoryKeyValueStorage InMemoryStorage,
	KeyValueStorageSettings Settings,
	ISerializer Serializer,
	ISettings UnpackagedSettings)
	: ApplicationDataKeyValueStorage(EncryptedLogger, InMemoryStorage, Settings, Serializer, UnpackagedSettings)
{
	public new const string Name = "EncryptedApplicationData";

	private readonly DataProtectionProvider _provider = new DataProtectionProvider(DataProtectionProviderDescriptor);

	private const string DataProtectionProviderDescriptor = "LOCAL=user";

	/// <inheritdoc />
	public override bool IsEncrypted => false;


#nullable disable
	protected override async Task<T> GetTypedValue<T>(object encryptedData, CancellationToken ct) 
	{
		if (encryptedData is byte[] byteData)
		{

			var encryptedBuffer = CryptographicBuffer.CreateFromByteArray(byteData);
			var decryptedBuffer = await _provider.UnprotectAsync(encryptedBuffer).AsTask(ct);
			var data = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decryptedBuffer);

			var decryptedData = Deserialize<T>(data);
			return decryptedData;
		}

		return default;
	}
#nullable restore
	protected override async Task<object> GetObjectValue<T>(T value, CancellationToken ct)
	{
		var data = Serializer.ToString(value);
		var decryptedBuffer = CryptographicBuffer.ConvertStringToBinary(data, BinaryStringEncoding.Utf8);
		var encryptedBuffer = await _provider.ProtectAsync(decryptedBuffer).AsTask(ct);

		CryptographicBuffer.CopyToByteArray(encryptedBuffer, out var encryptedData);

		return encryptedData;
	}

}
#endif
