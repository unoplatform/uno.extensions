namespace Uno.Extensions.Maui.Internals;

internal class MauiContentHost : Microsoft.Maui.Controls.ContentView
{
	public MauiContentHost(ResourceDictionary resources)
	{
		Resources = resources.ToMauiResources();
		HorizontalOptions = LayoutOptions.Fill;
		VerticalOptions = LayoutOptions.Fill;
	}
}
