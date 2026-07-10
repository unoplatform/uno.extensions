using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.NavViewShell;

/// <summary>Second tab content for <see cref="NavViewShellPage"/> tests.</summary>
public sealed partial class NavMenuPage : Page
{
	public NavMenuPage()
	{
		Content = new TextBlock { Text = "Menu" };
	}
}
