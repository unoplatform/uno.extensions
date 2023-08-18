using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui;

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
			.UseMauiEmbedding<TApp>()
			.RegisterPlatformServices(app);

		mauiAppBuilder.Services.AddSingleton<Microsoft.UI.Xaml.Application>(_ => app)
			.AddSingleton<IMauiInitializeService, MauiEmbeddingInitializer>();

		// HACK: https://github.com/dotnet/maui/pull/16758
		mauiAppBuilder.Services.RemoveAll<IApplication>()
			.AddSingleton<IApplication, TApp>();

		configure?.Invoke(mauiAppBuilder);

		var mauiApp = mauiAppBuilder.Build();
		mauiApp.InitializeMauiEmbeddingApp();
#endif
		return app;
	}

	private static void InitializeScopedServices(this IMauiContext scopedContext)
	{
		var scopedServices = scopedContext.Services.GetServices<IMauiInitializeScopedService>();

		foreach (var service in scopedServices)
		{
			service.Initialize(scopedContext.Services);
		}
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
