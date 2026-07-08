namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for code-behind navigation tests. The method body is modified at runtime
/// to simulate updating the route used in a code-behind navigation call.
/// </summary>
internal static class HotReloadCodeBehindNavTarget
{
	internal static string GetRoute()
	{
		return "PageOne";
	}
}
