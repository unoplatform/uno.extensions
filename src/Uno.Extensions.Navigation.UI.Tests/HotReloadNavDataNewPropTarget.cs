#if DEBUG
namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for the nav-data new-property test (#3080).
/// Before HR: returns null (property not populated).
/// After HR: returns a value (property becomes populated).
/// </summary>
internal static class HotReloadNavDataNewPropTarget
{
	internal static string? GetNewProperty() => null;
}
#endif
