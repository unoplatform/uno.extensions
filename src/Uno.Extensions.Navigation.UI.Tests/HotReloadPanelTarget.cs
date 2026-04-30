namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Dedicated HR target for the inline-Panel navigation test
/// (<c>HowTo-UsePanel</c> pattern: pre-existing children switched via
/// <c>Region.Navigator="Visibility"</c>). Kept separate from
/// <see cref="HotReloadTarget"/> / <see cref="HotReloadRegionTarget"/> so the
/// dev-server's delta cache for one scenario does not bleed into another.
/// </summary>
internal static class HotReloadPanelTarget
{
	internal static string GetValue()
	{
		return "original";
	}
}
