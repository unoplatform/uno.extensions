using UIKit;
using Uno.Extensions.Maui.Platform;
using Windows.UI.Core;

namespace Uno.Extensions.Maui;

partial class MauiEmbedding
{
	// NOTE: This is meant to help initialize MauiEmbedding similar to MauiUIApplicationDelegate
	// https://github.com/dotnet/maui/blob/ace9fe5e7d8d9bd16a2ae0b2fe2b888ad681433e/src/Core/src/Platform/iOS/MauiUIApplicationDelegate.cs#L36-L70
	private static MauiAppBuilder RegisterPlatformServices(this MauiAppBuilder builder, Application app)
	{
		builder.Services.AddSingleton<UIWindow>(sp =>
			{
				var window = sp.GetRequiredService<Microsoft.UI.Xaml.Window>();
				// The _window field is the only way to grab the underlying UIWindow from inside the CoreWindow
				// https://github.com/unoplatform/uno/blob/34a32058b812a0a08e658eba5e298ea9d258c231/src/Uno.UWP/UI/Core/CoreWindow.iOS.cs#L17
				var internalWindow = typeof(CoreWindow).GetField("_window", BindingFlags.Instance | BindingFlags.NonPublic);
				if(internalWindow is null)
				{
					throw new MauiEmbeddingException(Properties.Resources.MissingWindowPrivateField);
				}
				var uiwindow = internalWindow?.GetValue(window.CoreWindow) as UIWindow;
				return uiwindow!;
			})
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
			throw new MauiEmbeddingException(Properties.Resources.TheApplicationMustInheritFromEmbeddingApplication);
		}

		Microsoft.Maui.ApplicationModel.Platform.Init(() => mauiApp.Services.GetRequiredService<UIWindow>().RootViewController!);
		embeddingApp.InitializeApplication(mauiApp.Services, iApp);
		app.SetApplicationHandler(iApp, rootContext);
		InitializeApplicationMainPage(iApp);
	}
}
