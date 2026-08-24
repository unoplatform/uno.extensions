using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Target page for the #3072 new-route test. When the route is successfully
/// registered at runtime, navigation should land here.
/// </summary>
// partial: on the Android head the Uno tooling emits a second declaration for Page subclasses,
// which is CS0260 without it. Harmless on the other targets.
public sealed partial class HotReloadNewRoutePage : Page
{
	public HotReloadNewRoutePage()
	{
		Content = new TextBlock { Text = "NewRoutePage loaded" };
	}

	public string DisplayedValue => "new-route-loaded";
}
