#if DEBUG
namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for the VM constructor parameter reorder test (#3082).
/// Before HR: combines parameters as "first-second".
/// After HR: combines parameters as "second-first" (reversed order).
/// </summary>
internal static class HotReloadParamOrderTarget
{
	internal static string Combine(string first, string second)
	{
		return $"{first}-{second}";
	}
}
#endif
