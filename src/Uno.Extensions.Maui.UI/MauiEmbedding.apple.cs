#if IOS || MACCATALYST
using UIKit;

namespace Uno.Extensions.Maui;

partial class MauiEmbedding
{
	private static MauiAppBuilder RegisterPlatformServices(this MauiAppBuilder builder, Application app)
	{
		builder.Services.AddTransient<UIWindow>(sp => sp.GetRequiredService<Application>().Window!)
			.AddSingleton<IUIApplicationDelegate>(sp => sp.GetRequiredService<Application>());

		return builder;
	}

	private static void InitializeMauiEmbeddingApp(this MauiApp mauiApp)
	{
		var rootContext = new MauiContext(mauiApp.Services);
		rootContext.InitializeScopedServices();

		var iApp = mauiApp.Services.GetRequiredService<IApplication>();
		IPlatformApplication.Current = new EmbeddingApp(mauiApp.Services, iApp);
		var appDelegate = mauiApp.Services.GetRequiredService<IUIApplicationDelegate>();
		appDelegate.SetApplicationHandler(iApp, rootContext);
	}
}
#endif
