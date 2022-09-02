
namespace Uno.Extensions.Authentication;

public interface ITokenCache
{
	ValueTask<string?> CurrentProviderAsync(CancellationToken ct);
	ValueTask<bool> HasTokenAsync(CancellationToken cancellationToken );
	ValueTask<IDictionary<string, string>> GetAsync(CancellationToken cancellationToken);
	ValueTask SaveAsync(string provider, IDictionary<string, string>? tokens, CancellationToken cancellationToken);
	ValueTask ClearAsync(CancellationToken cancellationToken);
	event EventHandler? Cleared;
}
