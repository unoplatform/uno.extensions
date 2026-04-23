using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI.Tests.ViewModels;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Content page displayed inside each tab's region slot. Reads from
/// <see cref="HotReloadTabBarVm.DisplayedValue"/> which delegates to
/// <see cref="HotReloadTabBarTarget.GetValue"/> on every access.
/// </summary>
public sealed partial class HotReloadTabContentPage : Page
{
	private readonly TextBlock _text;

	public HotReloadTabContentPage()
	{
		_text = new TextBlock();
		Content = _text;
		DataContextChanged += OnDataContextChanged;
	}

	public string? DisplayedValue => (DataContext as HotReloadTabBarVm)?.DisplayedValue;

	private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		_text.Text = (args.NewValue as HotReloadTabBarVm)?.DisplayedValue;
	}
}
