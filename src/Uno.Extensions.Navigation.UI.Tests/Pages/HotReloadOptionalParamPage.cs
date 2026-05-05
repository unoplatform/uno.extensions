#if DEBUG
using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Simple page for the optional VM ctor param HR test (#3081).
/// Bound to <see cref="ViewModels.HotReloadOptionalParamVm"/>.
/// </summary>
internal sealed partial class HotReloadOptionalParamPage : Page
{
	public HotReloadOptionalParamPage()
	{
		Content = new TextBlock { Text = "OptionalParamPage" };
	}
}
#endif
