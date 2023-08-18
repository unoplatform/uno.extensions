#if IOS || MACCATALYST
using UIKit;

namespace Uno.Extensions.Maui;

partial class MauiEmbedding
{
	// NOTE: This is meant to help initialize MauiEmbedding similar to MauiUIApplicationDelegate
	// https://github.com/dotnet/maui/blob/ace9fe5e7d8d9bd16a2ae0b2fe2b888ad681433e/src/Core/src/Platform/iOS/MauiUIApplicationDelegate.cs#L36-L70
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
