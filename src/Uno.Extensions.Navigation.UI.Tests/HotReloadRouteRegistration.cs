namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// C# hot-reload target that controls whether TabThree is registered
/// in the route map. Before HR: returns <c>false</c>, after HR: returns <c>true</c>.
/// Used by <c>Given_TabBar_HotReload</c> Test 12 (that class is <c>#if DEBUG</c>, so a cref to it
/// does not resolve in Release).
/// </summary>
internal static class HotReloadRouteRegistration
{
	internal static bool IncludeTabThree() => false;
}
