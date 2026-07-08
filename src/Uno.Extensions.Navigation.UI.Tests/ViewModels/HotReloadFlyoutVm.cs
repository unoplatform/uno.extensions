#if DEBUG
namespace Uno.Extensions.Navigation.UI.Tests.ViewModels;

/// <summary>
/// ViewModel for the hot-reload flyout test (#3079).
/// Calls <see cref="HotReloadFlyoutTarget.ComputeLabel"/> in its constructor
/// and exposes the value. A static field tracks the most recent label so the
/// test can verify it without poking into popup internals.
/// </summary>
internal sealed class HotReloadFlyoutVm
{
	public static string? LastLabel { get; set; }

	public string Label { get; }

	public HotReloadFlyoutVm()
	{
		Label = HotReloadFlyoutTarget.ComputeLabel();
		LastLabel = Label;
	}
}
#endif
