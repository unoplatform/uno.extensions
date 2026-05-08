namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for Navigation.Request tests. The method body is modified at runtime
/// to simulate changing the navigation route string.
/// </summary>
internal static class HotReloadNavigationRequestTarget
{
	internal static string GetRoute()
	{
		return "PageOne";
	}
}
