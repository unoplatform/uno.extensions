namespace Uno.Extensions.Authentication;

internal record TokenCache : ITokenCache
{
	private readonly ILogger _logger;
	private readonly SemaphoreSlim tokenLock = new SemaphoreSlim(1);
	private readonly IKeyValueStorage _secureCache;

	public TokenCache(
		ILogger<TokenCache> logger,
		IKeyValueStorage secureCache)
	{
		_logger = logger;
		_secureCache = secureCache;
	}

	public event EventHandler? Cleared;

	public async ValueTask<string?> GetCurrentProviderAsync(CancellationToken ct)
	{
		await tokenLock.WaitAsync();
		try
		{
			return await _secureCache.GetStringAsync(nameof(GetCurrentProviderAsync), ct);
		}
		finally
		{
			tokenLock.Release();
		}
	}


	public async ValueTask ClearAsync(CancellationToken cancellation)
	{
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage("Clearing tokens by invoking SaveAsync with empty dictionary");
		// Don't acquire lock since this is done in the Save method
		await SaveAsync(string.Empty, new Dictionary<string, string>(), cancellation);
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage("Tokens cleared");
		try
		{
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage("Raising Cleared event");
			Cleared?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			if (_logger.IsEnabled(LogLevel.Error)) _logger.LogErrorMessage($"Error raising Cleared event - check listeners to fix errors handling this event {ex.Message}");
		}
	}

	public async ValueTask<IDictionary<string, string>> GetAsync(CancellationToken cancellation)
	{
		await tokenLock.WaitAsync();
		try
		{
			var all  = await _secureCache.GetAllValuesAsync(cancellation);
			if (all.ContainsKey(nameof(GetCurrentProviderAsync)))
			{
				all.Remove(nameof(GetCurrentProviderAsync));
			}
			return all;
		}
		finally
		{
			tokenLock.Release();
		}
	}

	public async ValueTask<bool> HasTokenAsync(CancellationToken cancellation)
	{
		await tokenLock.WaitAsync();
		try
		{
			var keys = await _secureCache.GetKeysAsync(cancellation);
			keys = keys.Where(x => x != nameof(GetCurrentProviderAsync)).ToArray();
			if (_logger.IsEnabled(LogLevel.Trace))
			{
				await LogKeyValues(keys, cancellation);
			}
			return keys.Any();
		}
		finally
		{
			tokenLock.Release();
		}
	}

	private async Task LogKeyValues(string[] keys, CancellationToken cancellation)
	{
		_logger.LogTraceMessage($"{keys.Length} keys in cache");
		foreach (var key in keys)
		{
			if (key is null)
			{
				continue;
			}
			try
			{
				var value = await _secureCache.GetAsync<string>(key, cancellation);
				_logger.LogTraceMessage($">{key}{value}");
			}
			catch
			{
				_logger.LogTraceMessage($">Unable to log {key} (it may not be a string value)");
			}
		}

	}

	public async ValueTask SaveAsync(string provider, IDictionary<string, string>? tokens, CancellationToken cancellation)
	{
		await tokenLock.WaitAsync();
		try
		{
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Save tokens ({tokens?.Count ?? 0}) for provider '{provider}' - start");
			await _secureCache.ClearAllAsync(cancellation);
			await _secureCache.SetAsync(nameof(GetCurrentProviderAsync), provider, cancellation);
			if (tokens is not null)
			{
				foreach (var tk in tokens)
				{
					await _secureCache.SetAsync(tk.Key, tk.Value, cancellation);
				}
			}
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage("Save tokens - complete");
		}
		finally
		{
			tokenLock.Release();
		}
	}
}
