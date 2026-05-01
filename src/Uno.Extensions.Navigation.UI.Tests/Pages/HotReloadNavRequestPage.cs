using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// XAML-backed page with a Button that uses Navigation.Request attached property.
/// XAML HR modifies the Navigation.Request value in the .xaml file.
/// </summary>
public sealed partial class HotReloadNavRequestPage : Page
{
	public HotReloadNavRequestPage()
	{
		this.InitializeComponent();
	}

	public Button NavigationButton => (Button)FindName("_navButton");
}
