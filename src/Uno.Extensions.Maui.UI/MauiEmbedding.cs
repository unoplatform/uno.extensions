using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Uno.Extensions.Hosting;
using Uno.Extensions.Maui.Extensibility;
using Uno.Extensions.Maui.Platform;

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
	public static IApplicationBuilder UseMauiEmbedding<TApp>(this IApplicationBuilder builder, Action<MauiAppBuilder>? configure = null)
		where TApp : MauiApplication
		=> builder.Configure(hostBuilder => hostBuilder.UseMauiEmbedding<TApp>(builder.App, builder.Window, configure));

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
#if MAUI_EMBEDDING
		var mauiAppBuilder = ConfigureMauiAppBuilder<TApp>(app, window, configure);
		builder.UseServiceProviderFactory(new UnoServiceProviderFactory(mauiAppBuilder, () => BuildMauiApp(mauiAppBuilder, app, window)));
#endif
		return builder;
	}

	/// <summary>
	/// Registers Maui embedding with WinUI3 and WPF application builder.
	/// </summary>
	/// <param name="app">The Uno app.</param>
	/// <param name="window">The Main Application Window.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static MauiApp UseMauiEmbedding<TApp>(this Microsoft.UI.Xaml.Application app, Microsoft.UI.Xaml.Window window, Action<MauiAppBuilder>? configure = null)
		where TApp : MauiApplication
	{
#if MAUI_EMBEDDING
		var mauiAppBuilder = ConfigureMauiAppBuilder<TApp>(app, window, configure);
		return BuildMauiApp(mauiAppBuilder, app, window);
#else
		return default!;
#endif
	}

#if MAUI_EMBEDDING

	private static MauiAppBuilder ConfigureMauiAppBuilder<TApp>(Application app, Microsoft.UI.Xaml.Window window, Action<MauiAppBuilder>? configure)
		where TApp : MauiApplication
	{
		// Forcing hot reload to false to prevent exceptions being raised
		Microsoft.Maui.HotReload.MauiHotReloadHelper.IsEnabled = false;

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

		return mauiAppBuilder;
	}

	private static MauiApp BuildMauiApp(MauiAppBuilder builder, Application app, Microsoft.UI.Xaml.Window window)
	{
		return MauiEmbeddingExtension.Default.BuildMauiApp(builder, app, window);
	}
#endif

	internal record EmbeddedApplication : IPlatformApplication
	{
		public EmbeddedApplication(IServiceProvider services, IApplication application)
		{
			Services = services;
			Application = application;
			IPlatformApplication.Current = this;
		}

		public IServiceProvider Services { get; }
		public IApplication Application { get; }
	}

	private static MauiAppBuilder RegisterPlatformServices(this MauiAppBuilder builder, Application app)
	{
		MauiEmbeddingExtension.Default.RegisterPlatformServices(builder, app);
		return builder;
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
