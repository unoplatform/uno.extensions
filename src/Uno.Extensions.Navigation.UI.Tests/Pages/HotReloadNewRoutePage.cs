using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Target page for the #3072 new-route test. When the route is successfully
/// registered at runtime, navigation should land here.
/// </summary>
public sealed class HotReloadNewRoutePage : Page
{
	public HotReloadNewRoutePage()
	{
		Content = new TextBlock { Text = "NewRoutePage loaded" };
	}

	public string DisplayedValue => "new-route-loaded";
}
