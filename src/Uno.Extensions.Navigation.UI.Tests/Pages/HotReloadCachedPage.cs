using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Page used to test NavigationCacheMode=Enabled with HR.
/// Cached pages should survive back-navigation without going blank.
/// </summary>
public sealed partial class HotReloadCachedPage : Page
{
	public HotReloadCachedPage()
	{
		NavigationCacheMode = NavigationCacheMode.Enabled;
		DisplayedValue = HotReloadTarget.GetValue();
		Content = new TextBlock { Text = DisplayedValue };
	}

	public string DisplayedValue { get; }
}
