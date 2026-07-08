namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target controlling whether the FirstPage/SecondPage nested routes are
/// registered inside <c>HotReloadMainTabBarPage</c>. Flipping this from
/// <c>false</c> to <c>true</c> via C# hot-reload simulates the developer
/// completing route registration after authoring the TabBar XAML and the new
/// page files. Kept in its own file so the
/// <c>"return false;" -&gt; "return true;"</c> patch is unambiguous and does
/// not collide with other gate targets in this project.
/// </summary>
internal static class HotReloadMainPageRouteGate
{
	internal static bool IsAvailable()
	{
		return false;
	}
}
