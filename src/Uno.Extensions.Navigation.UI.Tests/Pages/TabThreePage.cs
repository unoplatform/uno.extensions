using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// A page whose type name matches the route "TabThree" via the
/// <see cref="RouteResolverDefault"/> auto-resolve convention
/// (route + "Page" suffix = "TabThreePage").
/// Used by Test 13 to verify that auto-resolved routes work
/// with dynamically added TabBarItems.
/// </summary>
public sealed class TabThreePage : Page
{
	public TabThreePage()
	{
		Content = new TextBlock { Text = "TabThree auto-resolved" };
	}
}
