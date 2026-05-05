#if DEBUG
using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Simple page used as flyout content for the #3079 HR test.
/// Bound to <see cref="ViewModels.HotReloadFlyoutVm"/>.
/// </summary>
internal sealed partial class HotReloadFlyoutPage : Page
{
	public HotReloadFlyoutPage()
	{
		Content = new TextBlock { Text = "FlyoutPage" };
	}
}
#endif
