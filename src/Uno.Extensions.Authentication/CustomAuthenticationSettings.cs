
namespace Uno.Extensions.Authentication;

public record CustomAuthenticationSettings
(
	Func<IDispatcher, ITokenRepository, IDictionary<string, string>, Task<bool>> LoginCallback,
	Func<ITokenRepository, Task<bool>>? RefreshCallback = null,
	Func<IDispatcher, ITokenRepository, Task<bool>>? LogoutCallback = null
);
