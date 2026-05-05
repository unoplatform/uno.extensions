namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for Frame.Navigate from event handler test (#3078).
/// The method body is modified at runtime to simulate changing
/// the route navigated to from a button click event handler.
/// </summary>
internal static class HotReloadFrameNavTarget
{
	internal static string GetRoute()
	{
		return "PageOne";
	}
}
