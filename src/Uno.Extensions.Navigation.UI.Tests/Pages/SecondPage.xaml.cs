using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Companion tab page to <see cref="FirstPage"/>. Starts as a placeholder
/// TextBlock; XAML HR rewrites its <c>Text</c> to simulate post-TabBar page
/// authoring.
/// </summary>
public sealed partial class SecondPage : Page
{
	public SecondPage()
	{
		this.InitializeComponent();
	}

	public TextBlock Label => (TextBlock)FindName("_label");
}
