#if DEBUG
using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Simple page for the param-order HR test (#3082).
/// Bound to <see cref="ViewModels.HotReloadParamOrderVm"/>.
/// </summary>
internal sealed partial class HotReloadParamOrderPage : Page
{
	public HotReloadParamOrderPage()
	{
		Content = new TextBlock { Text = "ParamOrderPage" };
	}
}
#endif
