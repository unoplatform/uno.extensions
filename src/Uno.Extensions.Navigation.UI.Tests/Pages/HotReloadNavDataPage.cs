using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI.Tests.ViewModels;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Page used in navigation data contract tests. Bound to
/// <see cref="HotReloadNavDataVm"/> which receives <see cref="HotReloadNavData"/>.
/// </summary>
public sealed partial class HotReloadNavDataPage : Page
{
	private readonly TextBlock _text;

	public HotReloadNavDataPage()
	{
		_text = new TextBlock();
		Content = _text;
		DataContextChanged += OnDataContextChanged;
	}

	public string? DisplayedValue => (DataContext as HotReloadNavDataVm)?.DisplayedValue;
	public string? ExtraInfo => (DataContext as HotReloadNavDataVm)?.ReceivedData?.ExtraInfo;

	private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		if (args.NewValue is HotReloadNavDataVm vm)
		{
			_text.Text = $"{vm.DisplayedValue}|{vm.ReceivedData?.ExtraInfo ?? "null"}";
		}
	}
}
