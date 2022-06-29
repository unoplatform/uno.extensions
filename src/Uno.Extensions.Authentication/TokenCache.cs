
using Uno.Extensions.Configuration;

namespace Uno.Extensions.Authentication;

public record TokenCache : ITokenCache
{
	public TokenCache(IWritableOptions<TokensData> tokensCache)
	{
		_tokensCache = tokensCache;
		var tokens = _tokensCache.Value;
		if(tokens?.Tokens is not null)
		{
			_tokens = tokens.Tokens;
		}
	}

	private readonly IWritableOptions<TokensData> _tokensCache;

	private readonly IDictionary<string, string> _tokens = new Dictionary<string, string>();

	public event EventHandler? Cleared;

	public async ValueTask<bool> ClearAsync(CancellationToken? cancellation = default)
	{
		_tokens.Clear();
		await PersistCacheAsync();
		Cleared?.Invoke(this, EventArgs.Empty);
		return true;
	}
	public async ValueTask<IDictionary<string, string>> GetAsync(CancellationToken? cancellation = default) => _tokens;
	public async ValueTask<bool> HasTokenAsync(CancellationToken? cancellation = default) => _tokens.Count > 0;

	public async ValueTask<bool> SaveAsync(IDictionary<string, string>? tokens, CancellationToken? cancellation = default)
	{
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
		await _tokensCache.UpdateAsync(data => new TokensData { Tokens = _tokens });
	}
}

public record TokensData
{
	public IDictionary<string, string> Tokens { get; init; } = new Dictionary<string, string>();
}
