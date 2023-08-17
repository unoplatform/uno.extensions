#if ANDROID
namespace Uno.Extensions.Maui;

partial class MauiEmbedding
{
	private static MauiAppBuilder RegisterPlatformServices(this MauiAppBuilder builder, Application app)
	{
		if (Android.App.Application.Context is not Android.App.Application androidApp)
		{
			throw new MauiEmbeddingException($"Expected 'Android.App.Application.Context' to be 'Android.App.Application', but got '{Android.App.Application.Context.GetType().FullName}'.");
		}
		builder.Services.AddSingleton<Android.App.Application>(androidApp)
			.AddTransient<Android.App.Activity>(_ =>
			{
				if (UI.ContextHelper.Current is Android.App.Activity currentActivity)
					return currentActivity;

				throw new MauiEmbeddingException("Could not find a current Activity.");
			});
		return builder;
	}

	private static void InitializeMauiEmbeddingApp(this MauiApp mauiApp)
	{
		var androidApp = mauiApp.Services.GetRequiredService<Android.App.Application>();
		var scope = mauiApp.Services.CreateScope();
		var rootContext = new MauiContext(scope.ServiceProvider, androidApp);
		rootContext.InitializeScopedServices();

		var iApp = mauiApp.Services.GetRequiredService<IApplication>();
		IPlatformApplication.Current = new EmbeddingApp(scope.ServiceProvider, iApp);

		androidApp.SetApplicationHandler(iApp, rootContext);
	}
}
#endif
