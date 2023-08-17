namespace Uno.Extensions.Maui.Internals;

internal class MauiContentHost : Microsoft.Maui.Controls.ContentView
{
	public MauiContentHost(MauiResourceDictionary resources)
	{
		// Ensure that we properly parent to the Application so that resources can be found.
		if (IPlatformApplication.Current is not null)
		{
			var app = IPlatformApplication.Current.Application;
			if (app is Element element && app.Windows is List<Microsoft.Maui.Controls.Window> windows)
			{
				windows.Add(new Microsoft.Maui.Controls.Window
				{
					Parent = element,
					Page = new ContentPage
					{
						Content = this
					}
				});
			}
		}

		Resources = resources;
		HorizontalOptions = LayoutOptions.Fill;
		VerticalOptions = LayoutOptions.Fill;
	}
}
