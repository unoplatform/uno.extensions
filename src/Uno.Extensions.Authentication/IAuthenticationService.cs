namespace Uno.Extensions.Authentication;

public interface IAuthenticationService
{
	Task<bool> CanRefresh();
	Task<bool> LoginAsync(IDispatcher dispatcher, CancellationToken cancellationToken);
	Task<bool> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken);
	Task<bool> RefreshAsync(CancellationToken cancellationToken);
	Task<bool> LogoutAsync(IDispatcher dispatcher, CancellationToken cancellationToken);
}
