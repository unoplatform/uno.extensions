
namespace Uno.Extensions.Authentication;

public interface IReadonlyTokenCache
{
	ValueTask<bool> HasTokenAsync(CancellationToken? cancellationToken = default);
	ValueTask<IDictionary<string, string>> GetAsync(CancellationToken? cancellationToken = default);
}

public interface ITokenCache: IReadonlyTokenCache
{
	ValueTask<bool> SaveAsync(IDictionary<string, string>? tokens, CancellationToken? cancellationToken = default);
	ValueTask<bool> ClearAsync(CancellationToken? cancellationToken = default);
	event EventHandler? Cleared;
}
