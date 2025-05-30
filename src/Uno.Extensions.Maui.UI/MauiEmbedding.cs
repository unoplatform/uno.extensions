using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Uno.Extensions.Hosting;
using Uno.Extensions.Maui.Extensibility;
using Uno.Extensions.Maui.Platform;

#if !WINDOWS
using Uno.Foundation.Extensibility;
#endif

namespace Uno.Extensions.Maui;

/// <summary>
/// Embedding support for Microsoft.Maui controls in Uno Platform app hosts.
/// </summary>
public static partial class MauiEmbedding
{
	private static Lazy<IMauiInitExtension> _mauiInitExtension = new Lazy<IMauiInitExtension>(() =>
			{
#if !WINDOWS
				ApiExtensibility.CreateInstance<IMauiInitExtension>(typeof(MauiEmbedding), out var extension);
				return extension ?? new MauiInitExtension();
#else
				// On Windows, we don't use the extensibility system, so we can just return a new instance directly.
				return new MauiInitExtension();
#endif

			});
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
		var mauiApp = builder.Build();
		mauiApp.InitializeMauiEmbeddingApp(app);

#if WINDOWS
		window.Activated += (s, args) =>
		{
			WindowStateManager.Default.OnActivated(window, args);
		};
#endif
		return mauiApp;
	}

	

	private static void InitializeApplicationMainPage(IApplication iApp)
	{
		_mauiInitExtension.Value.Initialize(iApp);
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
		_mauiInitExtension.Value.RegisterPlatformServices(builder, app);
		return builder;
	}

	private static void InitializeMauiEmbeddingApp(this MauiApp mauiApp, Application app)
	{
		_mauiInitExtension.Value.InitializeMauiEmbeddingApp(mauiApp, app);
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
