namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for route registration switching tests (#3084).
/// Initially returns false; C# HR flips it to true so the route builder
/// re-registers with DataViewMap instead of ViewMap.
/// </summary>
internal static class HotReloadRouteSwitch
{
	internal static bool UseDataViewMap() => false;
}
