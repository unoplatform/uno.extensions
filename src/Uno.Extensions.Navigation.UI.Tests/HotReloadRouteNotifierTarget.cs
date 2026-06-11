namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for the IRouteNotifier handler test (#3089).
/// The method body is modified at runtime to simulate changing
/// the route-changed event handler logic.
/// </summary>
internal static class HotReloadRouteNotifierTarget
{
	internal static string ProcessRouteChange(string route)
	{
		return $"modified-{route}";
	}
}
