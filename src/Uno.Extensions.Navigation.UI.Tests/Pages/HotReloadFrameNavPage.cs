using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Page that performs Frame.Navigate from a button click event handler (#3078).
/// The target route is determined by <see cref="HotReloadFrameNavTarget.GetRoute"/>
/// which is modified via HR.
/// </summary>
public sealed partial class HotReloadFrameNavPage : Page
{
	public Button NavigateButton { get; }

	public HotReloadFrameNavPage()
	{
		NavigateButton = new Button { Content = "Frame.Navigate" };
		NavigateButton.Click += OnNavigateClick;
		Content = NavigateButton;
	}

	private async void OnNavigateClick(object sender, RoutedEventArgs e)
	{
		var route = HotReloadFrameNavTarget.GetRoute();
		await this.Navigator()!.NavigateRouteAsync(this, route);
	}
}
