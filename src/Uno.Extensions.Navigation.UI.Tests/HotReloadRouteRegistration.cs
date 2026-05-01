namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// C# hot-reload target that controls whether TabThree is registered
/// in the route map. Before HR: returns <c>false</c>, after HR: returns <c>true</c>.
/// Used by <see cref="Given_TabBarHotReload"/> Test 12.
/// </summary>
internal static class HotReloadRouteRegistration
{
	internal static bool IncludeTabThree() => false;
}
