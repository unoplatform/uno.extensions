namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Target for the route-init HR test. The method body is modified at runtime by
/// Given_HotReload.When_UpdateRouteInitGate_Then_GatedRouteBecomesNavigable to simulate a
/// previously-blocked route becoming navigable after hot-reload.
/// </summary>
internal static class HotReloadRouteGate
{
	internal static bool IsAvailable()
	{
		return true;
	}
}
