namespace Uno.Extensions.Navigation;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseNavigation(
		this IHostBuilder hostBuilder,
		Action<IViewRegistry, IRouteRegistry>? viewRouteBuilder,
		Func<IServiceCollection, IViewRegistry>? createViewRegistry,
		Func<IServiceCollection, IRouteRegistry>? createRouteRegistry,
		Func<NavigationConfig, NavigationConfig>? configure,
		Action<IServiceCollection> configureServices)
	{
		return hostBuilder.UseNavigation(
			viewRouteBuilder,
			createViewRegistry,
			createRouteRegistry,
			configure,
			(context, builder) => configureServices.Invoke(builder));
	}

	public static IHostBuilder UseNavigation(
		this IHostBuilder hostBuilder,
		Action<IViewRegistry, IRouteRegistry>? viewRouteBuilder = null,
		Func<IServiceCollection, IViewRegistry>? createViewRegistry = null,
		Func<IServiceCollection, IRouteRegistry>? createRouteRegistry = null,
		Func<NavigationConfig, NavigationConfig>? configure = null,
		Action<HostBuilderContext, IServiceCollection>? configureServices = default)
	{
		return hostBuilder
			.ConfigureServices((ctx,services) =>
			{
				_ = services.AddNavigation(configure, viewRouteBuilder, createViewRegistry, createRouteRegistry);
				configureServices?.Invoke(ctx, services);
			});
	}
}
