namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for the gated-tab test. The method body is modified at runtime by
/// <c>Given_TabBarHotReload.When_GatedTabUnlockedByHR_Then_TabContentLoads</c> to simulate
/// a third tab's content becoming available after hot-reload. Kept separate from
/// <see cref="HotReloadTabBarTarget"/> to avoid delta cache bleeding.
/// </summary>
internal static class HotReloadTabGateTarget
{
	internal static bool IsAvailable()
	{
		return false;
	}
}
