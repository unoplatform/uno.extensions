using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Target tab page used by the TabBar-late-add HR scenario. Starts as a
/// placeholder TextBlock; XAML HR rewrites its <c>Text</c> to simulate the
/// developer authoring the page after wiring the TabBar.
/// </summary>
public sealed partial class FirstPage : Page
{
	public FirstPage()
	{
		this.InitializeComponent();
	}

	public TextBlock Label => (TextBlock)FindName("_label");
}
