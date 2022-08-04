

namespace Uno.Extensions.Authentication;

public interface IAuthenticationFlow
{ 
	INavigator Navigator { get; }
	void Initialize(IDispatcher dispatcher, INavigator navigator);

	Task<NavigationResponse?> AuthenticatedNavigateAsync(NavigationRequest request, INavigator? navigator = default, CancellationToken ct = default);
	Task<bool> EnsureAuthenticatedAsync(CancellationToken ct = default);
	Task<bool> LoginAsync(IDictionary<string, string>? credentials, string? provider = null, CancellationToken ct = default);
	Task<bool> LogoutAsync(CancellationToken ct = default);
}
