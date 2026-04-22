namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Target class for the modal hot-reload scenario. The method body is modified at runtime
/// by Given_HotReload via HotReloadHelper. Kept separate from HotReloadTarget so the
/// underlying page's state (which uses HotReloadTarget) is provably untouched by a modal
/// HR edit.
/// </summary>
internal static class HotReloadModalTarget
{
	internal static string GetValue()
	{
		return "original";
	}
}
