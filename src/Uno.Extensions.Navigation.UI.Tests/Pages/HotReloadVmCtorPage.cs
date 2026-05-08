using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI.Tests.ViewModels;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Page for the VM constructor body logic test (#3083).
/// Bound to <see cref="HotReloadVmCtorVm"/> which calls
/// <see cref="HotReloadVmCtorTarget.ComputeValue"/> in its constructor.
/// </summary>
public sealed partial class HotReloadVmCtorPage : Page
{
	private readonly TextBlock _text;

	public HotReloadVmCtorPage()
	{
		_text = new TextBlock();
		Content = _text;
		DataContextChanged += OnDataContextChanged;
	}

	public string? DisplayedValue => (DataContext as HotReloadVmCtorVm)?.ComputedValue;

	private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		if (args.NewValue is HotReloadVmCtorVm vm)
		{
			_text.Text = vm.ComputedValue;
		}
	}
}
