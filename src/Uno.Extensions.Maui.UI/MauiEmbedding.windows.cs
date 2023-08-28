using Uno.Extensions.Maui.Platform;

namespace Uno.Extensions.Maui;

partial class MauiEmbedding
{
	//private static ResourceDictionary? _clone = null;
	// NOTE: This is meant to help initialize MauiEmbedding similar to MauiWinUIApplication
	// https://github.com/dotnet/maui/blob/ace9fe5e7d8d9bd16a2ae0b2fe2b888ad681433e/src/Core/src/Platform/Windows/MauiWinUIApplication.cs#L21-L49
	private static MauiAppBuilder RegisterPlatformServices(this MauiAppBuilder builder, Application app)
	{
		//_clone = app.Resources.Clone();
		//_ = builder.Services.RemoveWhere(sd =>
		//			sd.ServiceType == typeof(IMauiInitializeService) &&
		//								(
		//									// Match using Name since the types are internal to Maui
		//									sd.ImplementationType is { Name: "MauiControlsInitializer" } ||
		//									sd.ImplementationType is { Name: "MauiCoreInitializer" }
		//								));
		return builder;
	}

	private static void InitializeMauiEmbeddingApp(this MauiApp mauiApp, Application app)
	{
		var rootContext = new MauiContext(mauiApp.Services);
		rootContext.InitializeScopedServices();

		var iApp = mauiApp.Services.GetRequiredService<IApplication>();
		if (app is not EmbeddingApplication embeddingApp)
		{
			throw new MauiEmbeddingException(Properties.Resources.TheApplicationMustInheritFromEmbeddingApplication);
		}

		embeddingApp.InitializeApplication(mauiApp.Services, iApp);
		app.SetApplicationHandler(iApp, rootContext);
		InitializeApplicationMainPage(iApp);
	}
}
