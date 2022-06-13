
namespace Uno.Extensions.Authentication;

public record AuthenticationFlowSettings
(
	string? LoginRoute = null,
	Type? LoginView = null,
	Type? LoginViewModel = null,

	string? HomeRoute = null,
	Type? HomeView = null,
	Type? HomeViewModel = null,

		string? ErrorRoute = null,
	Type? ErrorView = null,
	Type? ErrorViewModel = null
);
