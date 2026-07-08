using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// XAML-backed page with a Button that uses Navigation.Data attached property.
/// XAML HR modifies the Navigation.Data value in the .xaml file (#3077).
/// </summary>
public sealed partial class HotReloadNavDataBindingPage : Page
{
	public HotReloadNavDataBindingPage()
	{
		this.InitializeComponent();
	}

	public Button NavigationButton => (Button)FindName("_navButton");
}
