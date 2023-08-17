using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui;

namespace Uno.Extensions.Maui;

/// <summary>
/// Embedding support for Microsoft.Maui controls in Uno Platform app hosts.
/// </summary>
public static class MauiEmbedding
{
#if MAUI_EMBEDDING
	private static MauiApp? _app;
	internal static IMauiContext MauiContext =>
#if ANDROID
		_app is not null ? new MauiContext(_app.Services, UI.ContextHelper.Current)
			: throw new MauiEmbeddingInitializationException();
#else
		_app is not null ? new MauiContext(_app.Services)
			: throw new MauiEmbeddingInitializationException();
#endif
#endif

	/// <summary>
	/// Registers Maui embedding in the Uno Platform app builder.
	/// </summary>
	/// <returns>The updated app builder.</returns>
	/// <param name="builder">The IHost builder.</param>
	/// <param name="app">The Uno app.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static IHostBuilder UseMauiEmbedding(this IHostBuilder builder, Microsoft.UI.Xaml.Application app, Action<MauiAppBuilder>? configure = null) =>
		builder.UseMauiEmbedding<MauiApplication>(app, configure);

	/// <summary>
	/// Registers Maui embedding in the Uno Platform app builder.
	/// </summary>
	/// <returns>The updated app builder.</returns>
	/// <param name="builder">The IHost builder.</param>
	/// <param name="app">The Uno app.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static IHostBuilder UseMauiEmbedding<TApp>(this IHostBuilder builder, Microsoft.UI.Xaml.Application app, Action<MauiAppBuilder>? configure = null)
		where TApp : MauiApplication
	{
		app.UseMauiEmbedding<TApp>(configure);
		return builder;
	}

	/// <summary>
	/// Registers Maui embedding with WinUI3 and WPF application builder.
	/// </summary>
	/// <param name="app">The Uno app.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static Microsoft.UI.Xaml.Application UseMauiEmbedding(this Microsoft.UI.Xaml.Application app, Action<MauiAppBuilder>? configure = null) =>
		app.UseMauiEmbedding<MauiApplication>(configure);

	/// <summary>
	/// Registers Maui embedding with WinUI3 and WPF application builder.
	/// </summary>
	/// <param name="app">The Uno app.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static Microsoft.UI.Xaml.Application UseMauiEmbedding<TApp>(this Microsoft.UI.Xaml.Application app, Action<MauiAppBuilder>? configure = null)
		where TApp : MauiApplication
	{
#if MAUI_EMBEDDING
		var mauiAppBuilder = MauiApp.CreateBuilder()
				.UseMauiEmbedding<TApp>();

		// HACK: https://github.com/dotnet/maui/pull/16758
		mauiAppBuilder.Services.RemoveAll<IApplication>()
			.AddSingleton<IApplication, TApp>();

#if WINDOWS
		_ = mauiAppBuilder.Services.RemoveWhere(sd =>
					sd.ServiceType == typeof(IMauiInitializeService) &&
										(
											// Match using Name since the types are internal to Maui
											sd.ImplementationType is { Name: "MauiControlsInitializer" } ||
											sd.ImplementationType is { Name: "MauiCoreInitializer" }
										));
#endif

		configure?.Invoke(mauiAppBuilder);

#if ANDROID
		if (Android.App.Application.Context is not Android.App.Application androidApp)
		{
			throw new InvalidOperationException($"Expected 'Android.App.Application.Context' to be 'Android.App.Application', but got '{Android.App.Application.Context.GetType().FullName}'.");
		}
		mauiAppBuilder.Services.AddSingleton<Android.App.Application>(androidApp)
			.AddTransient<Android.App.Activity>(_ =>
			{
				if (UI.ContextHelper.Current is Android.App.Activity currentActivity)
					return currentActivity;

				throw new InvalidOperationException("Could not find a current Activity.");
			});
#elif IOS || MACCATALYST
		mauiAppBuilder.Services.AddTransient<UIKit.UIWindow>(_ =>
			app.Window!)
			.AddSingleton<UIKit.IUIApplicationDelegate>(app);
#endif

		mauiAppBuilder.Services.AddSingleton<Microsoft.UI.Xaml.Application>(_ => app)
			.AddSingleton<IMauiInitializeService, MauiEmbeddingInitializer>();
		_app = mauiAppBuilder.Build();

		// Initialize the MauiApplication to ensure there is a Window and MainPage to ensure references to these will work.
		var iApp = _app.Services.GetRequiredService<IApplication>();
		IPlatformApplication.Current = new EmbeddingApp(_app.Services, iApp);
		if (iApp is MauiApplication mauiApplication)
		{
			mauiApplication.MainPage = new Microsoft.Maui.Controls.Page();
		}
#endif
		return app;
	}

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
