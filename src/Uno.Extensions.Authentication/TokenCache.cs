
namespace Uno.Extensions.Authentication;

public record TokenCache : ITokenCache
{
	private IDictionary<string, string> _tokens = new Dictionary<string, string>();

	public event EventHandler? Cleared;

	public Task ClearAsync()
	{
		_tokens.Clear();
		Cleared?.Invoke(this, EventArgs.Empty);
		return Task.CompletedTask;
	}
	public Task<IDictionary<string, string>> GetAsync() => Task.FromResult(_tokens);
	public Task<bool> HasTokenAsync() => Task.FromResult(_tokens.Count > 0);

	public Task SaveAsync(IDictionary<string, string> tokens)
	{
		foreach (var tk in tokens)
		{
			_tokens[tk.Key] = tk.Value;	
		}
		return Task.CompletedTask;
	}
}
