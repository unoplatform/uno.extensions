#if !WINUI && (__ANDROID__ || __IOS__ || WINDOWS_UWP)

namespace Uno.Extensions.Storage.KeyValueStorage;

public record PasswordVaultResourceNameProvider(string Name);

/// <summary>
/// Allows saving settings as passwords in the <see cref="PasswordVault"/>. This service has limitations, 
/// as no more than 10 entries can be added by a single application in the Credential Locker.
/// </summary>
internal record PasswordVaultKeyValueStorage(
	ILogger<PasswordVaultKeyValueStorage> Logger,
	ISerializer Serializer,
	PasswordVaultResourceNameProvider ResourceNameProvider) : IKeyValueStorage
{
	public const string Name = "PasswordVault";

	private readonly PasswordVault _passwordVault = new PasswordVault();

	/// <inheritdoc />
	/// <remarks>
	/// The Credential Locker encrypts what it stores and scopes it to the user - the whole reason to
	/// pick this over <see cref="ApplicationDataKeyValueStorage"/>. Corrected under spec 011 item 6,
	/// alongside <see cref="EncryptedApplicationDataKeyValueStorage"/>.
	/// </remarks>
	public bool IsEncrypted => true;

	/// <inheritdoc/>
	public async ValueTask ClearAsync(string? name, CancellationToken ct)
	{
		try
		{
			if (Logger.IsEnabled(LogLevel.Debug))
			{
				Logger.LogDebugMessage($"Clearing value for key '{name}'.");
			}

			var credential = _passwordVault
				.RetrieveAll()
				.FirstOrDefault(c => c.UserName.Equals(name, StringComparison.OrdinalIgnoreCase));

			if (credential != null)
			{
				_passwordVault.Remove(credential);
			}
		}
		catch
		{
			// Any error is ignored and the value is considered "absent".
		}

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Cleared value for key '{name}'.");
		}
	}

	/// <inheritdoc/>
	public async ValueTask<string[]> GetKeysAsync(CancellationToken ct)
	{
		return _passwordVault
			.RetrieveAll()
			.Select(c => c.UserName)
			.ToArray();
	}

	/// <inheritdoc/>
	public async ValueTask<T?> GetAsync<T>(string name, CancellationToken ct)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Getting value for key '{name}'.");
		}

		var credential = _passwordVault
			.RetrieveAll()
			.FirstOrDefault(c => c.UserName.Equals(name, StringComparison.OrdinalIgnoreCase));

		if (credential == null)
		{
			if (Logger.IsEnabled(LogLevel.Information))
			{
				Logger.LogInformationMessage($"Retrieved default value for key '{name}'.");
			}

			return default(T);
		}

		credential.RetrievePassword();

		var value = Serializer.FromString<T>(credential.Password);

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Retrieved value for key '{name}'.");
		}

		return value;
	}

	/// <inheritdoc/>
	public async ValueTask SetAsync<T>(string name, T value, CancellationToken ct) where T:notnull
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Setting value for key '{name}'.");
		}

		var data = Serializer.ToString(value, typeof(T));
		_passwordVault.Add(new PasswordCredential(ResourceNameProvider.Name, name, data));


		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Value for key '{name}' set.");
		}
	}
}
#endif
