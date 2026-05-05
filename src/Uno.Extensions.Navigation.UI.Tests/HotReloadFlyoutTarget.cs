#if DEBUG
namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for the flyout/modal VM test (#3079).
/// The method body is swapped via hot-reload to verify that a
/// re-shown flyout creates a new VM instance with updated logic.
/// </summary>
internal static class HotReloadFlyoutTarget
{
	internal static string ComputeLabel() => "flyout-v1";
}
#endif
