using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// ContentDialog used by <see cref="Given_HotReload"/>'s modal scenario. <see cref="DisplayedValue"/>
/// is captured from <see cref="HotReloadModalTarget.GetValue"/> in the ctor so a fresh dialog
/// instance picks up the HR'd method body when <c>ContentDialogNavigator</c> constructs it.
/// A static <see cref="Current"/> pointer lets the test reach the live dialog without having
/// to walk <c>XamlRoot</c>'s popup layer.
/// </summary>
public sealed partial class HotReloadModalDialog : ContentDialog
{
	public static HotReloadModalDialog? Current { get; private set; }

	public HotReloadModalDialog()
	{
		DisplayedValue = HotReloadModalTarget.GetValue();
		Content = new TextBlock { Text = DisplayedValue };
		Current = this;
		Closed += OnClosed;
	}

	public string DisplayedValue { get; }

	private void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
	{
		if (ReferenceEquals(Current, this))
		{
			Current = null;
		}
	}
}
