using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI.Tests.ViewModels;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

public sealed partial class HotReloadVmPage : Page
{
	private readonly TextBlock _text;

	public HotReloadVmPage()
	{
		_text = new TextBlock();
		Content = _text;
		DataContextChanged += OnDataContextChanged;
	}

	public string? DisplayedValue => (DataContext as HotReloadVm)?.DisplayedValue;

	private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		_text.Text = (args.NewValue as HotReloadVm)?.DisplayedValue;
	}
}
