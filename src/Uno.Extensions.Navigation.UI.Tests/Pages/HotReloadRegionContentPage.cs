using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI.Tests.ViewModels;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Content page used inside each region slot of the region-navigation HR test.
/// Mirrors <see cref="HotReloadVmPage"/> but binds to <see cref="HotReloadRegionVm"/> so the
/// region scenario owns its own HR target file.
/// </summary>
public sealed partial class HotReloadRegionContentPage : Page
{
	private readonly TextBlock _text;

	public HotReloadRegionContentPage()
	{
		_text = new TextBlock();
		Content = _text;
		DataContextChanged += OnDataContextChanged;
	}

	public string? DisplayedValue => (DataContext as HotReloadRegionVm)?.DisplayedValue;

	private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		_text.Text = (args.NewValue as HotReloadRegionVm)?.DisplayedValue;
	}
}
