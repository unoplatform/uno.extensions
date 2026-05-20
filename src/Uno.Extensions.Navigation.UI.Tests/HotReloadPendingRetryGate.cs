namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// C# hot-reload target controlling whether <c>HotReloadPendingRetryPage</c>
/// is constructible. Initially returns <c>false</c>, which causes the page's
/// constructor to throw — simulating the Studio Live scaffold-then-hot-reload
/// scenario where the initial navigation fires before the target type's
/// dependencies are present in the running assembly. HR flips the body to
/// <c>return true;</c>, after which the constructor no longer throws and
/// <see cref="UI.NavigationRouteUpdateHandler"/> must re-issue the pending
/// failed navigation request so the page lands in the visual tree without
/// a full app restart.
/// </summary>
internal static class HotReloadPendingRetryGate
{
	internal static bool IsAvailable()
	{
		return false;
	}
}
