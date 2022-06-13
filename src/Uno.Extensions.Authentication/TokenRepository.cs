
namespace Uno.Extensions.Authentication;

public record TokenRepository : ITokenRepository
{
	private IDictionary<string, string> _tokens = new Dictionary<string, string>();

	public event EventHandler? Cleared;

	public Task Clear()
	{
		_tokens.Clear();
		Cleared?.Invoke(this, EventArgs.Empty);
		return Task.CompletedTask;
	}
	public Task<IDictionary<string, string>> Get() => Task.FromResult(_tokens);
	public Task Save(IDictionary<string, string> tokens)
	{
		foreach (var tk in tokens)
		{
			_tokens[tk.Key] = tk.Value;	
		}
		return Task.CompletedTask;
	}
}
