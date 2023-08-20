using UIKit;
using Uno.Extensions.Maui.Platform;

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

	private static void InitializeMauiEmbeddingApp(this MauiApp mauiApp, Application app)
	{
		var rootContext = new MauiContext(mauiApp.Services);
		rootContext.InitializeScopedServices();

		var iApp = mauiApp.Services.GetRequiredService<IApplication>();
		if (app is not EmbeddingApplication embeddingApp)
		{
			throw new MauiEmbeddingException("The provided application must inherit from EmbeddingApplication");
		}

		// TODO: Evaluate getting the Root View Controller for a Platform.Init for Maui
		embeddingApp.InitializeApplication(mauiApp.Services, iApp);
		app.SetApplicationHandler(iApp, rootContext);
	}
}
