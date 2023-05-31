using Uno.Extensions.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Uno.Extensions.Maui.Internals;

namespace Uno.Extensions.Maui;

public static class MauiEmbedding
{
#if !NO_PLATFORM
	private static MauiApp? _app;
#endif
	internal static IMauiContext MauiContext =>
#if ANDROID
		_app is not null ? new MauiContext(_app.Services, UI.ContextHelper.Current)
			: throw new MauiEmbeddingInitializationException();
#elif NO_PLATFORM
		throw new PlatformNotSupportedException();
#else
	_app is not null ? new MauiContext(_app.Services)
			: throw new MauiEmbeddingInitializationException();
#endif

	public static IApplicationBuilder UseMauiEmbedding(this IApplicationBuilder builder, Action<MauiAppBuilder>? configure = null)
	{
		builder.App.UseMauiEmbedding(configure);
		return builder;
	}

#if NO_PLATFORM
	public static void UseMauiEmbedding(this Microsoft.UI.Xaml.Application app, Action<MauiAppBuilder>? configure = null)
	{
		throw new PlatformNotSupportedException();
	}
#else
	public static void UseMauiEmbedding(this Microsoft.UI.Xaml.Application app, Action<MauiAppBuilder>? configure = null)
	{
		var mauiAppBuilder = MauiApp.CreateBuilder()
				.UseMauiEmbedding<MauiApplication>();

		configure?.Invoke(mauiAppBuilder);

#if IOS || MACCATALYST
		mauiAppBuilder.Services.AddTransient<UIKit.UIWindow>(_ =>
			app.Window!);
#endif

		mauiAppBuilder.Services.AddSingleton(app)
			.AddSingleton<IMauiInitializeService, UnoMauiEmbeddingInitializer>();
		_app = mauiAppBuilder.Build();
	}
#endif

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
}
