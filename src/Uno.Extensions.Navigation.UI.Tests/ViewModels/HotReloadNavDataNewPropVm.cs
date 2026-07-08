#if DEBUG
namespace Uno.Extensions.Navigation.UI.Tests.ViewModels;

/// <summary>
/// Navigation data model for the #3080 test.
/// <see cref="NewProperty"/> starts as null (not populated) and becomes
/// non-null after HR changes the construction logic.
/// </summary>
public sealed record HotReloadNavDataWithNewProp(string Value, string? NewProperty = null);

/// <summary>
/// ViewModel for the #3080 nav-data new-property test.
/// Receives <see cref="HotReloadNavDataWithNewProp"/> via DI and exposes
/// the received data for assertions.
/// </summary>
public sealed class HotReloadNavDataNewPropVm
{
	public HotReloadNavDataNewPropVm(HotReloadNavDataWithNewProp? data = null)
	{
		ReceivedData = data;
	}

	public HotReloadNavDataWithNewProp? ReceivedData { get; }
}
#endif
