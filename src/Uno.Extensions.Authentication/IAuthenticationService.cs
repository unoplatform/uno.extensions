namespace Uno.Extensions.Authentication;

public interface IAuthenticationService
{
	string[] Providers { get; }
	ValueTask<bool> CanRefresh(CancellationToken? cancellationToken = default);
	ValueTask<bool> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials = default, string? provider = null, CancellationToken? cancellationToken = default);
	ValueTask<bool> RefreshAsync(CancellationToken? cancellationToken = default);
	ValueTask<bool> LogoutAsync(IDispatcher dispatcher, CancellationToken? cancellationToken = default);
}
