
using System.Collections.Immutable;

namespace Uno.Extensions.Authentication;

public record TokenCache : ITokenCache
{
	private readonly ILogger _logger;
	private readonly IWritableOptions<TokensData> _tokensCache;
	private readonly IDictionary<string, string> _tokens = new Dictionary<string, string>();
	private string? _provider;

	public TokenCache(
		ILogger<TokenCache> logger,
		IWritableOptions<TokensData> tokensCache)
	{
		_logger = logger;
		_tokensCache = tokensCache;
		var tokens = _tokensCache.Value;
		if (tokens?.Tokens is not null)
		{
			_tokens = tokens.Tokens;
			_provider = tokens.Provider;
		}
	}
	public string? CurrentProvider => _provider;


	public event EventHandler? Cleared;

	public async ValueTask ClearAsync(CancellationToken? cancellation = default)
	{
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage("Clearing tokens by invoking SaveAsync with empty dictionary");
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
	public async ValueTask<IDictionary<string, string>> GetAsync(CancellationToken? cancellation = default) => _tokens.ToDictionary(x => x.Key, x => x.Value);
	public async ValueTask<bool> HasTokenAsync(CancellationToken? cancellation = default) => _tokens.Count > 0;

	public async ValueTask SaveAsync(string provider, IDictionary<string, string>? tokens, CancellationToken? cancellation = default)
	{
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage($"Save tokens ({tokens?.Count ?? 0}) for provider '{provider}' - start");
		_provider = provider;
		_tokens.Clear();
		if (tokens is not null)
		{
			foreach (var tk in tokens)
			{
				_tokens[tk.Key] = tk.Value;
			}
		}
		await PersistCacheAsync();
		if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTraceMessage("Save tokens - complete");
	}

	private async ValueTask PersistCacheAsync()
	{
		await _tokensCache.UpdateAsync(data => new TokensData { Tokens = _tokens.ToImmutableDictionary(), Provider = _provider });
	}
}

public record TokensData
{
	public string? Provider { get; set; }
	public IDictionary<string, string> Tokens { get; set; } = new Dictionary<string, string>();
}
