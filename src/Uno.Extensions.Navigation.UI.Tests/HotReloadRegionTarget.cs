namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Dedicated HR target for the region-navigation test. Kept separate from
/// <see cref="HotReloadTarget"/> / <c>HotReloadVm</c> so the dev-server's delta cache
/// for one scenario does not bleed into another.
/// </summary>
internal static class HotReloadRegionTarget
{
	internal static string GetValue()
	{
		return "original";
	}
}
