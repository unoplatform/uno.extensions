
using Uno.Extensions.Configuration;

namespace Uno.Extensions.Authentication;

public record TokenCache : ITokenCache
{
	private readonly IWritableOptions<TokensData> _tokensCache;
	public TokenCache(IWritableOptions<TokensData> tokensCache)
	{
		_tokensCache = tokensCache;
		var tokens = _tokensCache.Value;
		if(tokens?.Tokens is not null)
		{
			_tokens = tokens.Tokens;
		}
	}

	private readonly IDictionary<string, string> _tokens = new Dictionary<string, string>();

	public event EventHandler? Cleared;

	public async Task ClearAsync()
	{
		_tokens.Clear();
		await PersistCacheAsync();
		Cleared?.Invoke(this, EventArgs.Empty);
	}
	public Task<IDictionary<string, string>> GetAsync() => Task.FromResult(_tokens);
	public Task<bool> HasTokenAsync() => Task.FromResult(_tokens.Count > 0);

	public async Task SaveAsync(IDictionary<string, string> tokens)
	{
		_tokens.Clear();
		foreach (var tk in tokens)
		{
			_tokens[tk.Key] = tk.Value;	
		}
		await PersistCacheAsync(); 
	}

	private async Task PersistCacheAsync()
	{
		await _tokensCache.UpdateAsync(data => new TokensData { Tokens = _tokens });
	}
}

public record TokensData
{
	public IDictionary<string, string> Tokens { get; init; } = new Dictionary<string, string>();
}
