using System.Text.Json.Serialization;

namespace Uno.Extensions.Authentication;

internal record TokenCache : ITokenCache
{
	private const string TokenPrefix = "AuthToken_";
	private readonly ILogger _logger;
	private readonly SemaphoreSlim tokenLock = new SemaphoreSlim(1);
	private readonly IKeyValueStorage _secureCache;

	public TokenCache(
		ILogger<TokenCache> logger,
		IKeyValueStorage secureCache)
	{
		_logger = logger;
		_secureCache = secureCache;

		// Access and refresh tokens are written here as-is. On a head whose default store is not
		// encrypted (Skia desktop on Windows/Linux, Skia-renderer Android/iOS, WebAssembly) that is a
		// cleartext store and nothing else says so - mirror the MSAL provider's warning so the app
		// author knows to register a protected IKeyValueStorage as the default. The in-memory store
		// persists nothing, so there is nothing to protect and nothing to warn about.
		if (!secureCache.IsEncrypted && secureCache is not InMemoryKeyValueStorage && logger.IsEnabled(LogLevel.Warning))
		{
			logger.LogWarningMessage($"Tokens are being stored in {secureCache.GetType().Name}, which does not protect its contents. Register an encrypted IKeyValueStorage as the default if the app sandbox is not enough on this platform");
		}
	}

	public event EventHandler? Cleared;

	private string CurrentProviderKey { get; } = $"{TokenPrefix}{nameof(GetCurrentProviderAsync)}";

	private bool TokenPrefixPredicate(string key) => key.StartsWith(TokenPrefix, StringComparison.InvariantCulture);

	public async ValueTask<string?> GetCurrentProviderAsync(CancellationToken ct)
	{
		await tokenLock.WaitAsync();
		try
		{
			return await _secureCache.GetStringAsync(CurrentProviderKey, ct);
		}
		finally
		{
			tokenLock.Release();
		}
	}


	public async ValueTask ClearAsync(CancellationToken cancellation)
	{
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage("Clearing tokens by invoking SaveAsync with empty dictionary");
		// Don't acquire lock since this is done in the Get/Save methods respectively
		var existingTokens = await GetAsync(cancellation);
		await SaveAsync(string.Empty, new Dictionary<string, string>(), cancellation);
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage("Tokens cleared");
		if (existingTokens.Any())
		{
			// Only triggered cleared event if there were actually tokens to be cleared
			// This prevents Cleared being raised when the user isn't logged in
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
	}

	public async ValueTask<IDictionary<string, string>> GetAsync(CancellationToken cancellation)
	{
		await tokenLock.WaitAsync();
		try
		{
			var all  = await _secureCache.GetAllValuesAsync(TokenPrefixPredicate, cancellation);
			if (all.ContainsKey(CurrentProviderKey))
			{
				all.Remove(CurrentProviderKey);
			}
			return all.ToDictionary(x=>x.Key.Replace(TokenPrefix,string.Empty),x=>x.Value);
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
			keys = keys.Where(x =>
							TokenPrefixPredicate(x) &&
							x != CurrentProviderKey).ToArray();
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

				// Log the shape, never the value: every entry here is an access, refresh or id
				// token, and this runs at Trace on a consumer's configured logging pipeline
				// (AGENTS.md §7). The length still distinguishes "present" from "empty", which is
				// all this diagnostic was ever useful for.
				_logger.LogTraceMessage($">{key} ({(value is null ? "no value" : $"{value.Length} chars")})");
			}
			catch (Exception ex)
			{
				_logger.LogTraceMessage($">Unable to log {key} ({ex.GetType().Name}; it may not be a string value)");
			}
		}

	}

	public async ValueTask SaveAsync(string provider, IDictionary<string, string>? tokens, CancellationToken cancellation)
	{
		await tokenLock.WaitAsync();
		try
		{
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Save tokens ({tokens?.Count ?? 0}) for provider '{provider}' - start");
			await _secureCache.ClearAllAsync(TokenPrefixPredicate, cancellation);
			await _secureCache.SetAsync(CurrentProviderKey, provider, cancellation);
			if (tokens is not null)
			{
				foreach (var tk in tokens)
				{
					await _secureCache.SetAsync($"{TokenPrefix}{tk.Key}", tk.Value, cancellation);
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

[JsonSerializable(typeof(string))]
internal partial class TokenCacheContext : JsonSerializerContext
{
}
