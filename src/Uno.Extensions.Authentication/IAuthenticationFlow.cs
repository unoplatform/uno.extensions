
namespace Uno.Extensions.Authentication;

public interface IAuthenticationFlow
{
	Task LaunchAsync();
	Task<bool> EnsureAuthenticatedAsync();
	Task<bool> LoginAsync(IDictionary<string, string>? credentials = null);
	Task<bool> LogoutAsync();
}
