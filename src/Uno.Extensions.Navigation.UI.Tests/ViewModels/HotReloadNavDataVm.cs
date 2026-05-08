namespace Uno.Extensions.Navigation.UI.Tests.ViewModels;

/// <summary>
/// ViewModel used in navigation data contract tests.
/// Receives data via constructor injection and exposes it for assertions.
/// </summary>
public sealed class HotReloadNavDataVm
{
	public HotReloadNavDataVm(HotReloadNavData? data = null)
	{
		ReceivedData = data;
	}

	public HotReloadNavData? ReceivedData { get; }

	public string DisplayedValue => ReceivedData?.Value ?? "no-data";
}

/// <summary>
/// Navigation data contract. The <see cref="ExtraInfo"/> property is populated
/// by <see cref="HotReloadNavDataTarget.GetExtraInfo"/> which is modified via HR.
/// </summary>
public sealed record HotReloadNavData(string Value, string? ExtraInfo = null);
