using Microsoft.Extensions.DependencyInjection.Extensions;

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
	public static IHostBuilder UseMauiEmbedding(this IHostBuilder builder, Microsoft.UI.Xaml.Application app, Action<MauiAppBuilder>? configure = null)
	{
		app.UseMauiEmbedding(configure);
		return builder;
	}

	/// <summary>
	/// Registers Maui embedding with WinUI3 and WPF application builder.
	/// </summary>
	/// <param name="app">The Uno app.</param>
	/// <param name="configure">Optional lambda to configure the Maui app builder.</param>
	public static Microsoft.UI.Xaml.Application UseMauiEmbedding(this Microsoft.UI.Xaml.Application app, Action<MauiAppBuilder>? configure = null)
	{
#if MAUI_EMBEDDING
		var mauiAppBuilder = MauiApp.CreateBuilder()
				.UseMauiEmbedding<MauiApplication>();


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

#if IOS || MACCATALYST
		mauiAppBuilder.Services.AddTransient<UIKit.UIWindow>(_ =>
			app.Window!);
#endif

		mauiAppBuilder.Services.AddSingleton(app)
			.AddSingleton<IMauiInitializeService, MauiEmbeddingInitializer>()
			.AddSingleton<MauiResourceManager>();
		_app = mauiAppBuilder.Build();
#endif
		return app;
	}

	/// <summary>
	/// When providing a <see cref="MauiResourceDictionary"/> with this method, the resources will be provided by default
	/// for all Maui controls.
	/// </summary>
	/// <typeparam name="TResources"></typeparam>
	/// <param name="maui"></param>
	/// <returns></returns>
	public static MauiAppBuilder UseMauiEmbeddingResources<TResources>(this MauiAppBuilder maui)
		where TResources : MauiResourceDictionary, new()
	{
		maui.Services.AddSingleton(new MauiResourceProvider(new TResources()));
		return maui;
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

internal record MauiResourceProvider(MauiResourceDictionary Resources);

internal record MauiResourceManager(IEnumerable<MauiResourceProvider> ResourceProviders)
{
	public MauiResourceDictionary CreateMauiResources(ResourceDictionary resources)
	{
#if MAUI_EMBEDDING
		var mauiResources = resources.ToMauiResources();
		if (ResourceProviders.Any())
		{
			ResourceProviders.Select(x => x.Resources)
				.ToList()
				.ForEach(x => mauiResources.MergedDictionaries.Add(x));
		}

		return mauiResources;
#else
		return new MauiResourceDictionary();
#endif
	}
}
