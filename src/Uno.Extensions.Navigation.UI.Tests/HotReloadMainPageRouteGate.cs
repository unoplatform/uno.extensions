namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target controlling whether the FirstPage/SecondPage nested routes are
/// registered inside <c>HotReloadMainTabBarPage</c>. Flipping this from
/// <c>false</c> to <c>true</c> via C# hot-reload simulates the developer
/// completing route registration after authoring the TabBar XAML and the new
/// page files. Kept in its own file so the hot-reload text patch of its return
/// statement matches exactly once; don't quote that statement in this comment,
/// or the patch and its revert hit the comment instead.
/// </summary>
internal static class HotReloadMainPageRouteGate
{
	internal static bool IsAvailable()
	{
		return false;
	}
}
