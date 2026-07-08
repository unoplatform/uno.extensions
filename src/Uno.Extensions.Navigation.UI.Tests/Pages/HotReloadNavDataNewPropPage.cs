#if DEBUG
using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Simple page for the nav-data new-property HR test (#3080).
/// Bound to <see cref="ViewModels.HotReloadNavDataNewPropVm"/>.
/// </summary>
internal sealed partial class HotReloadNavDataNewPropPage : Page
{
	public HotReloadNavDataNewPropPage()
	{
		Content = new TextBlock { Text = "NavDataNewPropPage" };
	}
}
#endif
