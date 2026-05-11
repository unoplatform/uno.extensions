using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// XAML-backed page used to test that ViewModel (DataContext) survives
/// XAML hot-reload view swap. The XAML is modified at runtime to trigger
/// ReplaceViewInstance, and the test asserts DataContext is re-injected.
/// </summary>
public sealed partial class HotReloadVmXamlPage : Page
{
	public HotReloadVmXamlPage()
	{
		this.InitializeComponent();
	}

	public TextBlock Label => (TextBlock)FindName("_label");
}
