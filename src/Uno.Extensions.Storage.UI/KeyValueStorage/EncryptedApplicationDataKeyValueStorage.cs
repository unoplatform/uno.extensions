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
		// byte[] on the packaged path, base64 on the unpackaged one - see GetObjectValue. Both are
		// accepted here rather than branching on UseApplicationData, so a value written before this
		// change still reads back whichever path it came from.
		var protectedBytes = encryptedData switch
		{
			byte[] bytes => bytes,
			string text when text is { Length: > 0 } => TryDecodeBase64(text),
			_ => null,
		};

		if (protectedBytes is null)
		{
			return default;
		}

		var encryptedBuffer = CryptographicBuffer.CreateFromByteArray(protectedBytes);
		var decryptedBuffer = await _provider.UnprotectAsync(encryptedBuffer).AsTask(ct);
		var data = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decryptedBuffer);

		var decryptedData = Deserialize<T>(data);
		return decryptedData;
	}
#nullable restore

	/// <summary>
	/// The protected bytes behind a base64 setting value, or <c>null</c> when it isn't base64.
	/// </summary>
	/// <remarks>
	/// Unpackaged installs written before this change hold the literal <c>"System.Byte[]"</c> - see
	/// <see cref="GetObjectValue{T}"/>. Those are unrecoverable by construction; treat them as absent
	/// rather than throwing on every read.
	/// </remarks>
	private static byte[]? TryDecodeBase64(string text)
	{
		try
		{
			return Convert.FromBase64String(text);
		}
		catch (FormatException)
		{
			return null;
		}
	}

	protected override async Task<object> GetObjectValue<T>(T value, CancellationToken ct)
	{
		var data = Serializer.ToString(value);
		var decryptedBuffer = CryptographicBuffer.ConvertStringToBinary(data, BinaryStringEncoding.Utf8);
		var encryptedBuffer = await _provider.ProtectAsync(decryptedBuffer).AsTask(ct);

		CryptographicBuffer.CopyToByteArray(encryptedBuffer, out var encryptedData);

		// Packaged installs keep persisting the raw byte[] - ApplicationData settings hold it
		// natively, it is what every existing install already contains, and this is the default
		// store on Windows, so it is not worth changing on a path that works.
		//
		// Unpackaged installs go through ISettings, which is string-only: the base class stores
		// whatever this returns via value?.ToString(), so a byte[] became the literal
		// "System.Byte[]" and DPAPI-protected values silently never persisted - while their keys
		// did, leaving HasTokenAsync true with nothing to recover. base64 round-trips there.
		return UseApplicationData
			? encryptedData
			: Convert.ToBase64String(encryptedData);
	}

}
#endif
