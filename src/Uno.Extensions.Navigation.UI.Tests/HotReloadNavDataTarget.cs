namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for the navigation data contract test. The method body is modified
/// at runtime to simulate adding/changing navigation data properties.
/// </summary>
internal static class HotReloadNavDataTarget
{
	internal static string GetExtraInfo()
	{
		return "original";
	}
}
