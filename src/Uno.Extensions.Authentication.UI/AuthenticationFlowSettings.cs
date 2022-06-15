
namespace Uno.Extensions.Authentication;

internal record AuthenticationFlowSettings
{
	public string? LoginRoute { get; init; }
	public Type? LoginView { get; init; }
	public Type? LoginViewModel { get; init; }

	public string? HomeRoute { get; init; }
	public Type? HomeView { get; init; }
	public Type? HomeViewModel { get; init; }

	public string? ErrorRoute { get; init; }
	public Type? ErrorView { get; init; }
	public Type? ErrorViewModel { get; init; }
}
