using FastAsyncLock = Uno.Extensions.Threading.FastAsyncLock;

namespace Uno.Extensions.Storage.KeyValueStorage;

internal record InMemoryKeyValueStorage(ILogger<InMemoryKeyValueStorage> Logger) : IKeyValueStorage
{
	public const string Name = "InMemory";

	private readonly FastAsyncLock _lock = new FastAsyncLock();
	private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

	/// <inheritdoc />
	public bool IsEncrypted => false;

	/// <inheritdoc />
	public async ValueTask ClearAsync(string? name, CancellationToken ct)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Clearing value for key '{name}'.");
		}

		using (await _lock.LockAsync(ct))
		{
			if (name is not null)
			{
				_values.Remove(name);
			}
			else
			{
				_values.Clear();
			}
		}

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Cleared value for key '{name}'.");
		}
	}

	/// <inheritdoc />
	public async ValueTask<T?> GetAsync<T>(string name, CancellationToken ct)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Getting value for key '{name}'.");
		}

		using (await _lock.LockAsync(ct))
		{
			if (_values.ContainsKey(name))
			{
				var value = (T)_values[name];

				if (Logger.IsEnabled(LogLevel.Information))
				{
					Logger.LogInformationMessage($"Retrieved value for key '{name}'.");
				}

				return value;
			}

			if (Logger.IsEnabled(LogLevel.Information))
			{
				Logger.LogInformationMessage($"Key '{name}' not found.");
			}

			return default(T?);
		}
	}

	/// <inheritdoc />
	public async ValueTask SetAsync<T>(string name, T value, CancellationToken ct) where T: notnull
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Setting value for key '{name}'.");
		}

		using (await _lock.LockAsync(ct))
		{
			_values[name] = value;
		}

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Value for key '{name}' set.");
		}
	}

	/// <inheritdoc />
	public async ValueTask<string[]> GetKeysAsync(CancellationToken ct)
	{
		using (await _lock.LockAsync(ct))
		{
			return _values.Keys.ToArray();
		}
	}

}
