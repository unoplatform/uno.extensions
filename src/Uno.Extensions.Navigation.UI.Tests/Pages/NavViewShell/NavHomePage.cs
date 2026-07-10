using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.NavViewShell;

/// <summary>Default ("home") tab content for <see cref="NavViewShellPage"/> tests.</summary>
public sealed partial class NavHomePage : Page
{
	public NavHomePage()
	{
		Content = new TextBlock { Text = "Home" };
	}
}
