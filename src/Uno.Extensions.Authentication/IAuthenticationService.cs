
namespace Uno.Extensions.Authentication;

public interface IAuthenticationService
{
	Task<bool> Login(IDispatcher dispatcher, IDictionary<string, string>? credentials = null);
	Task<bool> Refresh();
	Task<bool> Logout(IDispatcher dispatcher);
}
