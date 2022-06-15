
namespace Uno.Extensions.Authentication;

public record CustomAuthenticationSettings
(
	Func<IDispatcher, ITokenCache, IDictionary<string, string>, Task<bool>> LoginCallback,
	Func<ITokenCache, Task<bool>>? RefreshCallback = null,
	Func<IDispatcher, ITokenCache, Task<bool>>? LogoutCallback = null
);
