using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.TabNav;

/// <summary>
/// Simple page used for forward (non-tab) navigation test.
/// This route is nested under TabbedMain but is NOT a tab
/// (no matching NavigationViewItem exists in the TabSelector).
/// </summary>
public sealed partial class ForwardNavPage : Page
{
	public ForwardNavPage()
	{
		Content = new TextBlock { Text = "Forward Nav Page" };
	}
}
