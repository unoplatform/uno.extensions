using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.TabNav;

/// <summary>
/// Simple page used as content for Tab B.
/// </summary>
public sealed partial class TabBPage : Page
{
	public TabBPage()
	{
		Content = new TextBlock { Text = "Tab B Page" };
	}
}
