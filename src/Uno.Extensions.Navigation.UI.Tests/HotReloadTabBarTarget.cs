namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Dedicated HR target for TabBar hot-reload tests. Kept separate from
/// <see cref="HotReloadTarget"/> so the dev-server's delta cache for one
/// scenario does not bleed into another.
/// </summary>
internal static class HotReloadTabBarTarget
{
	internal static string GetValue()
	{
		return "original";
	}
}
