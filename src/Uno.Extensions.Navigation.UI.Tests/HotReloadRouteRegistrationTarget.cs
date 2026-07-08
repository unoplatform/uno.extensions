namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for #3072: Adding/removing a RouteMap at runtime.
/// Initially returns false; C# HR flips it to true to simulate
/// a developer adding a new route in the route registration code.
/// </summary>
internal static class HotReloadRouteRegistrationTarget
{
	internal static bool ShouldRegisterNewRoute() => false;
}
