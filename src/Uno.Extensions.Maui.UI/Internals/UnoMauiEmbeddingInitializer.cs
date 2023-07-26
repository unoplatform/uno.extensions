using Microsoft.Maui.Hosting;

namespace Uno.Extensions.Maui.Internals;

internal class UnoMauiEmbeddingInitializer : IMauiInitializeService
{
	private readonly Application _app;

	public UnoMauiEmbeddingInitializer(Application app)
	{
		_app = app;
	}

	public void Initialize(IServiceProvider services)
	{
		var resources = _app.Resources.ToMauiResources();
		var iApp = services.GetRequiredService<global::Microsoft.Maui.IApplication>();
		if (HasResources(resources) && iApp is MauiApplication mauiApp)
		{
			mauiApp.Resources.MergedDictionaries.Add(resources);
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
