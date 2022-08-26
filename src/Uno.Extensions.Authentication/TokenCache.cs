
using System.Collections.Immutable;
using Uno.Extensions.Configuration;

namespace Uno.Extensions.Authentication;

public record TokenCache : ITokenCache
{
	public TokenCache(IWritableOptions<TokensData> tokensCache)
	{
		_tokensCache = tokensCache;
		var tokens = _tokensCache.Value;
		if (tokens?.Tokens is not null)
		{
			_tokens = tokens.Tokens;
			_provider = tokens.Provider;
		}
	}
	public string? CurrentProvider => _provider;

	private readonly IWritableOptions<TokensData> _tokensCache;

	private readonly IDictionary<string, string> _tokens = new Dictionary<string, string>();
	private string? _provider;

	public event EventHandler? Cleared;

	public async ValueTask<bool> ClearAsync(CancellationToken? cancellation = default)
	{
		await SaveAsync(string.Empty,new Dictionary<string, string>(), cancellation);
		Cleared?.Invoke(this, EventArgs.Empty);
		return true;
	}
	public async ValueTask<IDictionary<string, string>> GetAsync(CancellationToken? cancellation = default) => _tokens.ToDictionary(x=>x.Key,x=>x.Value);
	public async ValueTask<bool> HasTokenAsync(CancellationToken? cancellation = default) => _tokens.Count > 0;

	public async ValueTask<bool> SaveAsync(string provider, IDictionary<string, string>? tokens, CancellationToken? cancellation = default)
	{
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
		return true;
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
