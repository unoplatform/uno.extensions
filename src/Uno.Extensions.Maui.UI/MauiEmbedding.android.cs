using Uno.Extensions.Maui.Platform;

namespace Uno.Extensions.Maui;

partial class MauiEmbedding
{
	// NOTE: This is meant to help initialize MauiEmbedding similar to what MauiApplication
	// https://github.com/dotnet/maui/blob/ace9fe5e7d8d9bd16a2ae0b2fe2b888ad681433e/src/Core/src/Platform/Android/MauiApplication.cs#L32-L53
	private static MauiAppBuilder RegisterPlatformServices(this MauiAppBuilder builder, Application app)
	{
		if (Android.App.Application.Context is not Android.App.Application androidApp)
		{
			throw new MauiEmbeddingException(string.Format(Properties.Resources.UnexpectedAndroidApplicationContextType, Android.App.Application.Context.GetType().FullName));
		}

		builder.Services.AddSingleton<Android.App.Application>(androidApp)
			.AddTransient<Android.Content.Context>(_ => UI.ContextHelper.Current)
			.AddTransient<Android.App.Activity>(_ =>
			{
				if (UI.ContextHelper.Current is Android.App.Activity currentActivity)
					return currentActivity;

				throw new MauiEmbeddingException(Properties.Resources.CouldNotFindCurrentActivity);
			});
		return builder;
	}

	private static void InitializeMauiEmbeddingApp(this MauiApp mauiApp, Application app)
	{
		var androidApp = mauiApp.Services.GetRequiredService<Android.App.Application>();
		var scope = mauiApp.Services.CreateScope();
		var rootContext = new MauiContext(scope.ServiceProvider, androidApp);
		rootContext.InitializeScopedServices();

		var iApp = mauiApp.Services.GetRequiredService<IApplication>();
		if(app is not EmbeddingApplication embeddingApp)
		{
			throw new MauiEmbeddingException(Properties.Resources.TheApplicationMustInheritFromEmbeddingApplication);
		}

		embeddingApp.InitializeApplication(scope.ServiceProvider, iApp);
		Microsoft.Maui.ApplicationModel.Platform.Init(androidApp);

		androidApp.SetApplicationHandler(iApp, rootContext);
		InitializeApplicationMainPage(iApp);
	}
}
