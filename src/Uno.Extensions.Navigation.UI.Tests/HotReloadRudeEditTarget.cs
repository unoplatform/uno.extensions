#if DEBUG
namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for the rude-edit resilience test (#3073/#3074).
/// The test will attempt an edit that changes this method's logic,
/// then verify that navigation remains functional regardless of
/// whether the edit was applied or rejected as a rude edit.
/// </summary>
internal static class HotReloadRudeEditTarget
{
	private static int _callCount;

	internal static string GetStableValue()
	{
		_callCount++;
		return "stable";
	}

	internal static int GetCallCount() => _callCount;

	internal static void ResetCallCount() => _callCount = 0;
}
#endif
