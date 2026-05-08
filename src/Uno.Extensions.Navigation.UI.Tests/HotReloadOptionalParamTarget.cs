#if DEBUG
namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for the optional VM constructor parameter test (#3081).
/// Before HR: ignores the optional service value.
/// After HR: incorporates the optional service value into the result.
/// </summary>
internal static class HotReloadOptionalParamTarget
{
	internal static string ComputeWithOptional(string baseValue, string? optionalInfo)
	{
		return baseValue;
	}
}
#endif
