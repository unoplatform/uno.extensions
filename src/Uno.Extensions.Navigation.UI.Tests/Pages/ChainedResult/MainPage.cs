using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.ChainedResult;

/// <summary>
/// Main page
/// In the real app this has a TabBar with 3 tabs. For tests, we use
/// a simple page since we navigate via INavigator, not UI controls.
/// </summary>
public sealed partial class MainPage : Page
{
	public MainPage()
	{
		Content = new TextBlock { Text = "Main Page" };
	}
}
