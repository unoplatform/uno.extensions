using Microsoft.Maui.ApplicationModel;

namespace Uno.Extensions.Maui.Internals;

internal class MauiEmbeddingInitializer : IMauiInitializeService
{
	private readonly Application _app;

	public MauiEmbeddingInitializer(Application app)
	{
		_app = app;
	}

	public void Initialize(IServiceProvider services)
	{
		var resources = _app.Resources.ToMauiResources();
		var iApp = services.GetRequiredService<global::Microsoft.Maui.IApplication>();
		if (iApp is MauiApplication mauiApp)
		{
			// Inject WinUI Resources to the MauiApplication
			if (HasResources(resources))
			{
				mauiApp.Resources.MergedDictionaries.Add(resources);
			}

			// Initialize the MauiApplication to ensure there is a Window and MainPage to ensure references to these will work.
			mauiApp.MainPage = new Microsoft.Maui.Controls.Page();

			// Make sure the requested app theme matches our app
			mauiApp.UserAppTheme = _app.RequestedTheme switch
			{
				ApplicationTheme.Dark => AppTheme.Dark,
				_ => AppTheme.Light
			};
		}
	}

	private static bool HasResources(MauiResourceDictionary resources)
	{
		if (resources.Keys.Any())
		{
			return true;
		}
		else if (resources.MergedDictionaries is not null && resources.MergedDictionaries.Any())
		{
			for (var i = 0; i < resources.MergedDictionaries.Count; i++)
			{
				if (HasResources(resources.MergedDictionaries.ElementAt(i)))
				{
					return true;
				}
			}
		}

		return false;
	}
}
