
namespace Uno.Extensions.Authentication;

public interface ITokenCache
{
	string? CurrentProvider { get; }
	ValueTask<bool> HasTokenAsync(CancellationToken? cancellationToken = default);
	ValueTask<IDictionary<string, string>> GetAsync(CancellationToken? cancellationToken = default);
	ValueTask<bool> SaveAsync(string provider, IDictionary<string, string>? tokens, CancellationToken? cancellationToken = default);
	ValueTask<bool> ClearAsync(CancellationToken? cancellationToken = default);
	event EventHandler? Cleared;
}
