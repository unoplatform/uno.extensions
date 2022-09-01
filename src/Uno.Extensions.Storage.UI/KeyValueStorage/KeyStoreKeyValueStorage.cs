#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Java.Security;
using Javax.Crypto;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Uno.Extensions.Threading;

namespace Uno.Extensions.Storage.KeyValueStorage;

/// <summary>
/// Allows saving settings in a secure storage using Android's <see cref="KeyStore"/>.
/// </summary>
internal record KeyStoreSettingsStorage : IKeyValueStorage
{
	public const string Name = "KeyStore";

	// It is not an issue if this password becomes public, as it's simply added encryption
	// above the app-level encryption.
	private const string DefaultPrivatePassword = "95407C28724B42F78A035C55987FDB21C7C2CB53529148B5A3021B715447E593";
	private const string DefaultFileName = "KeyStoreSettingsStorage";

	private readonly ILogger _logger;
	private readonly ISerializer _serializer;
	private readonly string _fileName = DefaultFileName;
	private readonly char[] _rootPassword  = DefaultPrivatePassword.ToCharArray();
	private readonly KeyStore.PasswordProtection _protection= new KeyStore.PasswordProtection(DefaultPrivatePassword.ToCharArray());

	private Lazy<KeyStore> _keyStoreSelector;

	// When changing and saving the KeyStore, we must protect parallel changes.
	private readonly FastAsyncLock _changeKeyStoreLock = new FastAsyncLock();

	private static readonly Encoding _utf8 = new UTF8Encoding(false);

	/// <summary>
	/// Creates a new <see cref="KeyStoreSettingsStorage"/> using a specific filename as the destination storage.
	/// </summary>
	public KeyStoreSettingsStorage(ILogger<KeyStoreSettingsStorage> logger, ISerializer serializer)
	{
		_serializer = serializer;
		_logger = logger;

		_keyStoreSelector = new Lazy<KeyStore>(LoadKeyStore);
	}

	/// <inheritdoc />
	public async ValueTask ClearAsync(string? name, CancellationToken ct)
	{
		if (_logger.IsEnabled(LogLevel.Debug))
		{
			_logger.LogDebugMessage($"Clearing value for key '{name}'.");
		}

		var keyStore = _keyStoreSelector.Value;

		using (await _changeKeyStoreLock.LockAsync(ct))
		{
			keyStore.DeleteEntry(name);
			SaveKeyStore(keyStore);
		}

		if (_logger.IsEnabled(LogLevel.Information))
		{
			_logger.LogInformationMessage($"Cleared value for key '{name}'.");
		}
	}

	/// <inheritdoc />
	public async ValueTask<string[]> GetKeysAsync(CancellationToken ct)
	{
		var aliases = _keyStoreSelector.Value.Aliases();

		var result = new List<string>();

		while (aliases?.HasMoreElements??false)
		{
			var item = aliases?.NextElement()?.ToString();
			if(item is not null){
				result.Add(item);
			}
		}

		return result.ToArray();
	}

	/// <inheritdoc />
	public async ValueTask<T?> GetAsync<T>(string name, CancellationToken ct)
	{
		if (_logger.IsEnabled(LogLevel.Debug))
		{
			_logger.LogDebugMessage($"Getting value for key '{name}'.");
		}

		var entry = _keyStoreSelector.Value.GetEntry(name, _protection) as KeyStore.SecretKeyEntry;

		if (entry is null)
		{
			throw new KeyNotFoundException(name);
		}

		var bytes = entry.SecretKey?.GetEncoded();
		if(bytes is null)
		{
			return default;
		}
		var value = _serializer.FromString<T>(_utf8.GetString(bytes));

		if (_logger.IsEnabled(LogLevel.Information))
		{
			_logger.LogInformationMessage($"Retrieved value for key '{name}'.");
		}

		return value;
	}


	/// <inheritdoc />
	public async ValueTask SetAsync<T>(string name, T value, CancellationToken ct) where T:notnull
	{
		if (_logger.IsEnabled(LogLevel.Debug))
		{
			_logger.LogDebugMessage($"Setting value for key '{name}'.");
		}

		var secret = new SecretKey(_serializer.ToString(value, typeof(T)));
		var entry = new KeyStore.SecretKeyEntry(secret);
		var keyStore = _keyStoreSelector.Value;

		using (await _changeKeyStoreLock.LockAsync(ct))
		{
			keyStore.SetEntry(name, entry, _protection);
			SaveKeyStore(keyStore);
		}

		if (_logger.IsEnabled(LogLevel.Information))
		{
			_logger.LogInformationMessage($"Value for key '{name}' set.");
		}
	}

	private KeyStore LoadKeyStore()
	{
		if (_logger.IsEnabled(LogLevel.Debug))
		{
			_logger.LogDebugMessage($"Loading keystore.");
		}

		var keyStore = KeyStore.GetInstance(KeyStore.DefaultType);

		if(keyStore is null)
		{
			this.Log().Error("Could not load keystore");
			throw new Exception("Unable to create keystore");
		}

		try
		{
			if (System.IO.File.Exists(_fileName))
			{
				if (_logger.IsEnabled(LogLevel.Debug))
				{
					_logger.LogDebugMessage($"Keystore file exists. Loading it.");
				}

				using (var stream = System.IO.File.OpenRead(_fileName))
				{
					keyStore.Load(stream, _rootPassword);
				}
			}
			else
			{
				if (_logger.IsEnabled(LogLevel.Debug))
				{
					_logger.LogDebugMessage($"Keystore file doesn't exist. Loading an empty store.");
				}

				keyStore.Load(null, _rootPassword);
			}
		}
		catch (Exception error)
		{
			if (_logger.IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Could not load keystore file. Loading an empty store.", error);
			}

			keyStore.Load(null, _rootPassword);
		}

		if (_logger.IsEnabled(LogLevel.Information))
		{
			_logger.LogInformationMessage($"Keystore loaded.");
		}

		return keyStore;
	}

	private void SaveKeyStore(KeyStore keyStore)
	{
		using (var stream = System.IO.File.OpenWrite(_fileName))
		{
			keyStore.Store(stream, _rootPassword);
		}
	}

	private class SecretKey : Java.Lang.Object, ISecretKey
	{
		private readonly string _data;

		public SecretKey(string data)
		{
			_data = data;
		}

		public string Algorithm => "RAW";

		public string Format => "RAW";

		public byte[] GetEncoded()
		{
			return _utf8.GetBytes(_data);
		}
	}
}
#endif
