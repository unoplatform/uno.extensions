#if WINDOWS
using Microsoft.Maui.Hosting;

namespace Uno.Extensions.Maui;

partial class MauiEmbedding
{
	// NOTE: This is meant to help initialize MauiEmbedding similar to MauiWinUIApplication
	// https://github.com/dotnet/maui/blob/ace9fe5e7d8d9bd16a2ae0b2fe2b888ad681433e/src/Core/src/Platform/Windows/MauiWinUIApplication.cs#L21-L49
	private static MauiAppBuilder RegisterPlatformServices(this MauiAppBuilder builder, Application app)
	{
		//_ = builder.Services.RemoveWhere(sd =>
		//			sd.ServiceType == typeof(IMauiInitializeService) &&
		//								(
		//									// Match using Name since the types are internal to Maui
		//									sd.ImplementationType is { Name: "MauiControlsInitializer" } ||
		//									sd.ImplementationType is { Name: "MauiCoreInitializer" }
		//								));
		return builder;
	}

	private static void InitializeMauiEmbeddingApp(this MauiApp mauiApp)
	{
		var rootContext = new MauiContext(mauiApp.Services);
		rootContext.InitializeScopedServices();

		var iApp = mauiApp.Services.GetRequiredService<IApplication>();
		IPlatformApplication.Current = new EmbeddingApp(mauiApp.Services, iApp);
		var app = mauiApp.Services.GetRequiredService<Application>();
		app.SetApplicationHandler(iApp, rootContext);
	}
}
#endif
