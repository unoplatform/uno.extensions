using System.ComponentModel;

#if !WINDOWS
using Uno.Foundation.Extensibility;
#endif

#if MAUI_RUNTIME_SKIA && !WINDOWS
[assembly: ApiExtension(typeof(IMauiEmbeddingExtension), typeof(SkiaMauiEmbeddingExtension))]
#endif

namespace Uno.Extensions.Maui.Extensibility;

/// <summary>
/// Runtime extension for Maui Embedding support in Skia-based Uno Platform applications
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class
#if MAUI_RUNTIME_SKIA
	SkiaMauiEmbeddingExtension()
#else
	MauiEmbeddingExtension()
#endif
	: IMauiEmbeddingExtension
{
	/// <summary>
	/// Initializes a new instance of the Maui Embedding runtime extension.
	/// </summary>
	/// <param name="_">An object parameter reserved for future use. Currently, it is not utilized.</param>
	public
#if MAUI_RUNTIME_SKIA
		SkiaMauiEmbeddingExtension
#else
		MauiEmbeddingExtension
#endif
		(object _) : this() { }

	private static readonly Lazy<IMauiEmbeddingExtension> _instance = new Lazy<IMauiEmbeddingExtension>(() =>
	{
#if !WINDOWS
		ApiExtensibility.CreateInstance<IMauiEmbeddingExtension>(typeof(MauiEmbedding), out var extension);
		return extension ?? new MauiEmbeddingExtension();
#else
		// On Windows we don't use the extensibility system, so we can just return a new instance directly.
		return new MauiEmbeddingExtension();
#endif
	});

	/// <summary>
	/// Gets the default instance of the Maui Embedding runtime extension.
	/// </summary>
	public static IMauiEmbeddingExtension Default => _instance.Value;

#if ANDROID || IOS || MACCATALYST || WINDOWS
	/// <inheritdoc/>
	public void Initialize(Microsoft.Maui.IApplication iApp)
	{
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
	}

	/// <inheritdoc/>
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

	/// <inheritdoc/>
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

	/// <inheritdoc/>
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
		return builder;
#else
		throw new PlatformNotSupportedException("MauiEmbedding is not supported on this platform.");
#endif
	}

	/// <inheritdoc/>
	public void OnSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		// Sanity Check
		if (IPlatformApplication.Current?.Application is null
			&& MauiApplication.Current?.Handler.MauiContext is not null)
		{
			_ = new MauiEmbedding.EmbeddedApplication(MauiApplication.Current.Handler.MauiContext.Services, MauiApplication.Current);
		}

		if (args.NewValue is null ||
			args.NewValue is not Type type ||
			!type.IsAssignableTo(typeof(VisualElement)) ||
			dependencyObject is not MauiHost mauiHost ||
			MauiApplication.Current?.Handler?.MauiContext is null)
		{
			return;
		}

		try
		{
			var app = MauiApplication.Current;
#if ANDROID
			var services = app.Handler.MauiContext.Services;
			var mauiContext = new MauiContext(services, services.GetRequiredService<Android.App.Activity>());
#else
			var mauiContext = MauiApplication.Current.Handler.MauiContext;
#endif
			// Allow the use of Dependency Injection for the View
			var instance = ActivatorUtilities.CreateInstance(mauiContext.Services, type);
			if(instance is VisualElement visualElement)
			{
				mauiHost.VisualElement = visualElement;
				visualElement.Parent = app.Windows[0];
				visualElement.BindingContext = mauiHost.DataContext;
			}
			else
			{
				throw new MauiEmbeddingException(string.Format(Properties.Resources.TypeMustInheritFromPageOrView, instance.GetType().FullName));
			}

			try
			{


				var native = visualElement.ToPlatform(mauiContext);
				mauiHost.Content = native;
			}
			catch (Exception ex)
			{
				var logger = mauiHost.GetLoggerInternal();
				if (logger.IsEnabled(LogLevel.Error))
				{
					logger.LogError(ex, Properties.Resources.UnableToConvertMauiViewToNativeView);
				}
				throw;
			}
			

			mauiHost.InvokeVisualElementChanged(visualElement);
		}
		catch (Exception ex)
		{
			var logger = mauiHost.GetLoggerInternal();
			if (logger.IsEnabled(LogLevel.Error))
			{
				logger.LogError(ex, Properties.Resources.UnableToConvertMauiViewToNativeView);
			}
#if DEBUG
			System.Diagnostics.Debugger.Break();
#endif
			throw new MauiEmbeddingException(Properties.Resources.UnexpectedErrorConvertingMauiViewToNativeView, ex);
		}
	}

	/// <inheritdoc/>
	public void OnSizeChanged(VisualElement? element)
	{
		element?.PlatformSizeChanged();
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
