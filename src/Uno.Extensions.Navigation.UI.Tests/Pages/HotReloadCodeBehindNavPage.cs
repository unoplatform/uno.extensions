using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Page that performs code-behind navigation. The target route is
/// determined by <see cref="HotReloadCodeBehindNavTarget.GetRoute"/>
/// which is modified via HR.
/// </summary>
public sealed partial class HotReloadCodeBehindNavPage : Page
{
	public Button NavigateButton { get; }

	public HotReloadCodeBehindNavPage()
	{
		NavigateButton = new Button { Content = "Navigate via Code" };
		NavigateButton.Click += OnNavigateClick;
		Content = NavigateButton;
	}

	private async void OnNavigateClick(object sender, RoutedEventArgs e)
	{
		var route = HotReloadCodeBehindNavTarget.GetRoute();
		await this.Navigator()!.NavigateRouteAsync(this, route);
	}
}
