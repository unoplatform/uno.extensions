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
					if (tk.Key != TokenCacheExtensions.IdTokenKey)
					{
						await _secureCache.SetAsync($"{TokenPrefix}{tk.Key}", tk.Value, cancellation);
					}
				}

				if (tokens.TryGetValue(TokenCacheExtensions.IdTokenKey, out var idToken))
				{
					await SaveIdTokenAsync(idToken, cancellation);
				}
			}
			if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage("Save tokens - complete");
		}
		finally
		{
			tokenLock.Release();
		}
	}

	/// <summary>
	/// Writes the ID token last and best-effort: it only carries claims for the app to read, so a
	/// store that rejects it must not fail a sign-in whose session tokens are already saved.
	/// </summary>
	/// <remarks>
	/// The case this exists for is packaged WinAppSDK, where <c>LocalSettings</c> caps a value at
	/// 8 KB and a claim-heavy ID token (B2C custom attributes, an Entra groups claim) exceeds it.
	/// Throwing there left the access token written - so <see cref="HasTokenAsync"/> true - while
	/// LoginAsync / RefreshAsync failed.
	/// </remarks>
	private async ValueTask SaveIdTokenAsync(string idToken, CancellationToken cancellation)
	{
		var key = $"{TokenPrefix}{TokenCacheExtensions.IdTokenKey}";
		try
		{
			await _secureCache.SetAsync(key, idToken, cancellation);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			// Length, never the value (AGENTS.md §7); the exception comes from the store, not the token.
			if (_logger.IsEnabled(LogLevel.Warning))
			{
				_logger.LogWarning(ex, "Unable to store the {Key} ({Length} chars), so the user's claims won't be readable from the token cache; the session is unaffected", TokenCacheExtensions.IdTokenKey, idToken.Length);
			}

			// The caching stores write their in-memory layer before the backing store, so a rejected
			// value would otherwise be readable until restart and then silently vanish.
			try
			{
				await _secureCache.ClearAsync(key, CancellationToken.None);
			}
			catch (Exception clearEx)
			{
				if (_logger.IsEnabled(LogLevel.Warning))
				{
					_logger.LogWarning(clearEx, "Unable to remove the partially stored {Key}", TokenCacheExtensions.IdTokenKey);
				}
			}
		}
	}
}

[JsonSerializable(typeof(string))]
internal partial class TokenCacheContext : JsonSerializerContext
{
}
