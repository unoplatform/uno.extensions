using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Flyout used by <see cref="Given_HotReload"/>'s flyout scenario. <see cref="DisplayedValue"/>
/// is captured from <see cref="HotReloadFlyoutTarget.GetValue"/> in the ctor so a fresh flyout
/// instance picks up the HR'd method body when <c>FlyoutNavigator</c> constructs it.
/// A static <see cref="Current"/> pointer lets the test reach the live flyout without having
/// to walk <c>XamlRoot</c>'s popup layer.
/// </summary>
public sealed partial class HotReloadFlyoutView : Flyout
{
	public static HotReloadFlyoutView? Current { get; private set; }

	public HotReloadFlyoutView()
	{
		DisplayedValue = HotReloadFlyoutTarget.GetValue();
		Content = new TextBlock { Text = DisplayedValue };
		Current = this;
		Closed += OnClosed;
	}

	public string DisplayedValue { get; }

	private void OnClosed(object? sender, object e)
	{
		if (ReferenceEquals(Current, this))
		{
			Current = null;
		}
	}
}
