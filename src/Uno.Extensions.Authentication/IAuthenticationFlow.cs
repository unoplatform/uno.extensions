
namespace Uno.Extensions.Authentication;

public interface IAuthenticationFlow
{
	Task Launch();
	Task<bool> EnsureAuthenticated();
	Task<bool> Login(IDictionary<string, string>? credentials = null);
	Task<bool> Logout();
}
