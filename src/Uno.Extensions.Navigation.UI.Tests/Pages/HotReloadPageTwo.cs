using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

public sealed partial class HotReloadPageTwo : Page
{
	public HotReloadPageTwo()
	{
		DisplayedValue = HotReloadTarget.GetValue();
		Content = new TextBlock { Text = DisplayedValue };
	}

	public string DisplayedValue { get; }
}
