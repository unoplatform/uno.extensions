using Microsoft.Extensions.DependencyInjection.Extensions;
using Uno.Extensions.Hosting;

namespace Uno.Extensions.Maui;

/// <summary>
/// Embedding support for Microsoft.Maui controls in Uno Platform app hosts.
/// </summary>
public static partial class MauiEmbedding
{
	/// <summary>
	/// Registers Maui embedding in the Uno Platform app builder.
	/// </summary>
	/// <returns>The updated app builder.</returns>
	/// <param name="builder">The IHost builder.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static IApplicationBuilder UseMauiEmbedding(this IApplicationBuilder builder, Action<MauiAppBuilder>? configure = null) =>
		builder.UseMauiEmbedding<MauiApplication>(configure);

	/// <summary>
	/// Registers Maui embedding in the Uno Platform app builder.
	/// </summary>
	/// <returns>The updated app builder.</returns>
	/// <param name="builder">The IHost builder.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static IApplicationBuilder UseMauiEmbedding<TApp>(this IApplicationBuilder builder, Action<MauiAppBuilder>? configure = null)
		where TApp : MauiApplication
	{
		builder.App.UseMauiEmbedding<TApp>(builder.Window, configure);
		return builder;
	}

	/// <summary>
	/// Registers Maui embedding in the Uno Platform app builder.
	/// </summary>
	/// <returns>The updated app builder.</returns>
	/// <param name="builder">The IHost builder.</param>
	/// <param name="app">The Uno app.</param>
	/// <param name="window">The Main Application Window.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static IHostBuilder UseMauiEmbedding(this IHostBuilder builder, Microsoft.UI.Xaml.Application app, Microsoft.UI.Xaml.Window window, Action<MauiAppBuilder>? configure = null) =>
		builder.UseMauiEmbedding<MauiApplication>(app, window, configure);

	/// <summary>
	/// Registers Maui embedding in the Uno Platform app builder.
	/// </summary>
	/// <returns>The updated app builder.</returns>
	/// <param name="builder">The IHost builder.</param>
	/// <param name="app">The Uno app.</param>
	/// <param name="window">The Main Application Window.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static IHostBuilder UseMauiEmbedding<TApp>(this IHostBuilder builder, Microsoft.UI.Xaml.Application app, Microsoft.UI.Xaml.Window window, Action<MauiAppBuilder>? configure = null)
		where TApp : MauiApplication
	{
		app.UseMauiEmbedding<TApp>(window, configure);
		return builder;
	}

	/// <summary>
	/// Registers Maui embedding with WinUI3 and WPF application builder.
	/// </summary>
	/// <param name="app">The Uno app.</param>
	/// <param name="window">The Main Application Window.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static Microsoft.UI.Xaml.Application UseMauiEmbedding(this Microsoft.UI.Xaml.Application app, Microsoft.UI.Xaml.Window window, Action<MauiAppBuilder>? configure = null) =>
		app.UseMauiEmbedding<MauiApplication>(window, configure);

	/// <summary>
	/// Registers Maui embedding with WinUI3 and WPF application builder.
	/// </summary>
	/// <param name="app">The Uno app.</param>
	/// <param name="window">The Main Application Window.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static Microsoft.UI.Xaml.Application UseMauiEmbedding<TApp>(this Microsoft.UI.Xaml.Application app, Microsoft.UI.Xaml.Window window, Action<MauiAppBuilder>? configure = null)
		where TApp : MauiApplication
	{
#if MAUI_EMBEDDING
		var mauiAppBuilder = MauiApp.CreateBuilder()
			.UseMauiEmbedding<TApp>()
			.RegisterPlatformServices(app);

		mauiAppBuilder.Services.AddSingleton(app)
			.AddSingleton(window)
			.AddSingleton<IMauiInitializeService, MauiEmbeddingInitializer>();

		// HACK: https://github.com/dotnet/maui/pull/16758
		mauiAppBuilder.Services.RemoveAll<IApplication>()
			.AddSingleton<IApplication, TApp>();

		configure?.Invoke(mauiAppBuilder);

		var mauiApp = mauiAppBuilder.Build();
		mauiApp.InitializeMauiEmbeddingApp(app);
#endif
		return app;
	}

#if MAUI_EMBEDDING

	private static void InitializeScopedServices(this IMauiContext scopedContext)
	{
		var scopedServices = scopedContext.Services.GetServices<IMauiInitializeScopedService>();

		foreach (var service in scopedServices)
		{
			service.Initialize(scopedContext.Services);
		}
	}

	private static void InitializeApplicationMainPage(IApplication iApp)
	{
		if (iApp is not MauiApplication app || app.Handler?.MauiContext is null)
		{
			// NOTE: This method is supposed to be called immediately after we initialize the Application Handler
			// This should never actually happen but is required due to nullability
			return;
		}

		var context = app.Handler.MauiContext;

		// Create an Application Main Page and initialize a Handler with the Maui Context
		var page = new ContentPage();
		app.MainPage = page;
		_ = page.ToPlatform(context);

		// Create a Maui Window and initialize a Handler shim. This will expose the actual Application Window
		var virtualWindow = new Microsoft.Maui.Controls.Window(page);
		virtualWindow.Handler = new EmbeddedWindowHandler
		{
#if IOS || MACCATALYST
			PlatformView = context.Services.GetRequiredService<Microsoft.UI.Xaml.Application>().Window,
#elif ANDROID
			PlatformView = context.Services.GetRequiredService<Android.App.Activity>(),
#elif WINDOWS
			PlatformView = context.Services.GetRequiredService<Microsoft.UI.Xaml.Window>(),
#endif
			VirtualView = virtualWindow,
			MauiContext = context
		};

		app.SetCoreWindow(virtualWindow);
	}

	private static void SetCoreWindow(this IApplication app, Microsoft.Maui.Controls.Window window)
	{
		if(app.Windows is List<Microsoft.Maui.Controls.Window> windows)
		{
			windows.Add(window);
		}
	}

#endif

	// NOTE: This was part of the POC and is out of scope for the MVP. Keeping it in case we want to add it back later.
	/*
	public static MauiAppBuilder MapControl<TWinUI, TMaui>(this MauiAppBuilder builder)
		where TWinUI : FrameworkElement
		where TMaui : Microsoft.Maui.Controls.View
	{
		Interop.MauiInterop.MapControl<TWinUI, TMaui>();
		return builder;
	}

	public static MauiAppBuilder MapStyleHandler<THandler>(this MauiAppBuilder builder)
		where THandler : Interop.IWinUIToMauiStyleHandler, new()
	{
		Interop.MauiInterop.MapStyleHandler<THandler>();
		return builder;
	}
	*/
}
