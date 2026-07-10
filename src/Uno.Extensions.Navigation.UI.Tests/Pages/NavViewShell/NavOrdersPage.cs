using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.NavViewShell;

/// <summary>Third tab content for <see cref="NavViewShellPage"/> tests.</summary>
public sealed partial class NavOrdersPage : Page
{
	public NavOrdersPage()
	{
		Content = new TextBlock { Text = "Orders" };
	}
}
