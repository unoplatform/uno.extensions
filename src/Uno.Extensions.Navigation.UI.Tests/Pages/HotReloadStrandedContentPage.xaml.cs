using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Placeholder page for the stranded-default-page HR scenario (uno.extensions#3130).
/// Mirrors studio.live's scaffolded page stub: a centered TextBlock whose text the
/// test rewrites via XAML hot reload while the page is live but not materialized.
/// </summary>
public sealed partial class HotReloadStrandedContentPage : Page
{
	public HotReloadStrandedContentPage()
	{
		this.InitializeComponent();
	}

	public TextBlock? Status => FindName("StatusText") as TextBlock;
}
