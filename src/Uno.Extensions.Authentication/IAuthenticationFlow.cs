

namespace Uno.Extensions.Authentication;

public interface IAuthenticationFlow
{
	void Initialize(IDispatcher dispatcher, INavigator navigator);
	Task<bool> EnsureAuthenticatedAsync(CancellationToken ct);
	Task<bool> LoginAsync(IDictionary<string, string>? credentials, CancellationToken ct);
	Task<bool> LogoutAsync(CancellationToken ct);
}
