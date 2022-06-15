
namespace Uno.Extensions.Authentication;

public interface IAuthenticationService
{
	Task<bool> LoginAsync(IDispatcher dispatcher, IDictionary<string, string>? credentials = null);
	Task<bool> RefreshAsync();
	Task<bool> Logout(IDispatcher dispatcher);
}
