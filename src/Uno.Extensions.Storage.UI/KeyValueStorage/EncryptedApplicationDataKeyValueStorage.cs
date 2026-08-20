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
	/// <remarks>
	/// Values are DPAPI-protected by <see cref="DataProtectionProvider"/> before they reach the
	/// settings container - see <see cref="GetObjectValue{T}"/>. This reported <c>false</c> until
	/// spec 011 item 6: the property is on the public <see cref="IKeyValueStorage"/> surface and is
	/// exactly the flag a consumer would branch on to decide whether a store is safe for tokens, so
	/// under-reporting it pushes callers away from the one Windows store that does protect them.
	/// </remarks>
	public override bool IsEncrypted => true;


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
