using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Page with a Button that uses Navigation.Request attached property.
/// The request string is set programmatically so we can test XAML HR changes.
/// </summary>
public sealed partial class HotReloadNavRequestPage : Page
{
	public Button NavigationButton { get; }

	public HotReloadNavRequestPage()
	{
		var grid = new Grid();
		Region.SetAttached(grid, true);

		NavigationButton = new Button { Content = "Navigate" };
		Navigation.SetRequest(NavigationButton, HotReloadNavigationRequestTarget.GetRoute());
		grid.Children.Add(NavigationButton);

		Content = grid;
	}
}
