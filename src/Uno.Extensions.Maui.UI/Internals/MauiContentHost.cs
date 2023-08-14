namespace Uno.Extensions.Maui.Internals;

internal class MauiContentHost : Microsoft.Maui.Controls.ContentView
{
	public MauiContentHost(MauiResourceDictionary resources)
	{
		Resources = resources;
		HorizontalOptions = LayoutOptions.Fill;
		VerticalOptions = LayoutOptions.Fill;
	}
}
