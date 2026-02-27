using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.TabNav;

/// <summary>
/// Simple page used as content for Tab A.
/// </summary>
public sealed partial class TabAPage : Page
{
	public TabAPage()
	{
		Content = new TextBlock { Text = "Tab A Page" };
	}
}
