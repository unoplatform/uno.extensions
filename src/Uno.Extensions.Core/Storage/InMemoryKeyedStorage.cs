using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Threading;

namespace Uno.Extensions.Storage;

public record InMemoryKeyedStorage(ILogger<InMemoryKeyedStorage> Logger) : IKeyedStorage
{
	public const string Name = "InMemory";

	private readonly FastAsyncLock _lock = new FastAsyncLock();
	private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

	/// <inheritdoc />
	public async ValueTask Clear(string? name, CancellationToken ct)
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
	public async ValueTask<T> GetValue<T>(string name, CancellationToken ct)
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

			throw new KeyNotFoundException($"Key '{name}' not found");
		}
	}

	/// <inheritdoc />
	public async ValueTask SetValue<T>(string name, T value, CancellationToken ct) where T: notnull
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
	public async ValueTask<bool> ContainsKey(string key, CancellationToken ct)
	{
		var keys = await GetAllKeys(ct);
		return keys.Contains(key);
	}


	/// <inheritdoc />
	public async ValueTask<string[]> GetAllKeys(CancellationToken ct)
	{
		using (await _lock.LockAsync(ct))
		{
			return _values.Keys.ToArray();
		}
	}

}
