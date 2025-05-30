using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Uno.Extensions.Maui.Internals;

namespace Uno.Extensions.Maui.Extensibility;

internal class MauiInitExtension : IMauiInitExtension
{
#if ANDROID || IOS || MACCATALYST || WINDOWS
	public void Initialize(Microsoft.Maui.IApplication iApp)
	{
		#if IOS || MACCATALYST || ANDROID || WINDOWS
			if (iApp is not MauiApplication app || app.Handler?.MauiContext is null)
			{
				// NOTE: This method is supposed to be called immediately after we initialize the Application Handler
				// This should never actually happen but is required due to nullability
				return;
			}

	#if ANDROID
			var services = app.Handler.MauiContext.Services;
			var context = new MauiContext(services, services.GetRequiredService<Android.App.Activity>());
	#else
			var context = app.Handler.MauiContext;
	#endif

			// Create an Application Main Page and initialize a Handler with the Maui Context
			var page = new ContentPage();
			app.MainPage = page;

			_ = page.ToPlatform(context);

			// Create a Maui Window and initialize a Handler shim. This will expose the actual Application Window
			var virtualWindow = new Microsoft.Maui.Controls.Window();
			virtualWindow.Handler = new EmbeddedWindowHandler
			{
	#if IOS || MACCATALYST
				PlatformView = context.Services.GetRequiredService<UIKit.UIWindow>(),
	#elif ANDROID
				PlatformView = context.Services.GetRequiredService<Android.App.Activity>(),
	#elif WINDOWS
				PlatformView = context.Services.GetRequiredService<Microsoft.UI.Xaml.Window>(),
	#endif
				VirtualView = virtualWindow,
				MauiContext = context
			};
			virtualWindow.Page = page;

			if (app.Windows is List<Microsoft.Maui.Controls.Window> windows)
			{
				windows.Add(virtualWindow);
			}
		#endif
	}

	public MauiApp BuildMauiApp(MauiAppBuilder builder, Application app, Microsoft.UI.Xaml.Window window)
	{
		var mauiApp = builder.Build();
		InitializeMauiEmbeddingApp(mauiApp, app);

#if WINDOWS
		window.Activated += (s, args) =>
		{
			WindowStateManager.Default.OnActivated(window, args);
		};
#endif
		return mauiApp;
	}

	public void InitializeMauiEmbeddingApp(MauiApp mauiApp, Application app)
	{
#if ANDROID
		var androidApp = mauiApp.Services.GetRequiredService<Android.App.Application>();
		var activity = mauiApp.Services.GetRequiredService<Android.App.Activity>();
		var scope = mauiApp.Services.CreateScope();
		var rootContext = new MauiContext(scope.ServiceProvider, androidApp);
		InitializeScopedServices(rootContext);

		var iApp = mauiApp.Services.GetRequiredService<IApplication>();
		_ = new MauiEmbedding.EmbeddedApplication(mauiApp.Services, iApp);

		// Initializing with the Activity to set the current activity.
		// The Bundle is not actually used by Maui
		Microsoft.Maui.ApplicationModel.Platform.Init(activity, null);

		androidApp.SetApplicationHandler(iApp, rootContext);
		Initialize(iApp);
#elif IOS || MACCATALYST
		var rootContext = new MauiContext(mauiApp.Services);
		InitializeScopedServices(rootContext);

		var iApp = mauiApp.Services.GetRequiredService<IApplication>();

		Microsoft.Maui.ApplicationModel.Platform.Init(() => mauiApp.Services.GetRequiredService<UIKit.UIWindow>().RootViewController!);
		_ = new MauiEmbedding.EmbeddedApplication(mauiApp.Services, iApp);
		UIKit.UIApplication.SharedApplication.Delegate.SetApplicationHandler(iApp, rootContext);
		Initialize(iApp);
#elif WINDOWS
		var rootContext = new MauiContext(mauiApp.Services);
		InitializeScopedServices(rootContext);

		var iApp = mauiApp.Services.GetRequiredService<IApplication>();
		_ = new MauiEmbedding.EmbeddedApplication(mauiApp.Services, iApp);
		app.SetApplicationHandler(iApp, rootContext);
		Initialize(iApp);
#else
		throw new PlatformNotSupportedException("MauiEmbedding is not supported on this platform.");
#endif
	}

	public MauiAppBuilder RegisterPlatformServices(MauiAppBuilder builder, Application app)
	{
#if ANDROID
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
#elif IOS || MACCATALYST
		builder.Services.AddTransient<UIKit.UIWindow>(sp =>
		{
			var window = sp.GetRequiredService<Microsoft.UI.Xaml.Window>();
			var nativeWindow = Uno.UI.Xaml.WindowHelper.GetNativeWindow(window);
			return nativeWindow is UIKit.UIWindow uiWindow ? uiWindow : throw new InvalidOperationException("Unable to locate the Native UIWindow");
		})
			.AddSingleton<UIKit.IUIApplicationDelegate>(sp => UIKit.UIApplication.SharedApplication.Delegate);

		return builder;
#elif WINDOWS
		//_clone = app.Resources.Clone();
		//_ = builder.Services.RemoveWhere(sd =>
		//			sd.ServiceType == typeof(IMauiInitializeService) &&
		//								(
		//									// Match using Name since the types are internal to Maui
		//									sd.ImplementationType is { Name: "MauiControlsInitializer" } ||
		//									sd.ImplementationType is { Name: "MauiCoreInitializer" }
		//								));
		return builder;
#else
		throw new PlatformNotSupportedException("MauiEmbedding is not supported on this platform.");
#endif
	}

	private void InitializeScopedServices(IMauiContext scopedContext)
	{
		var scopedServices = scopedContext.Services.GetServices<IMauiInitializeScopedService>();

		foreach (var service in scopedServices)
		{
			service.Initialize(scopedContext.Services);
		}
	}
#endif
}
