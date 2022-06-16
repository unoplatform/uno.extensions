
namespace Uno.Extensions.Authentication;

internal record AuthenticationFlowSettings
{
	public Func<INavigator, IDispatcher, Task>? LoginRequiredCallback { get; init; }
	public Func<INavigator, IDispatcher, Task>? LoginCompletedCallback { get; init; }
	public Func<INavigator, IDispatcher, Task>? LogoutCallback { get; init; }
}
