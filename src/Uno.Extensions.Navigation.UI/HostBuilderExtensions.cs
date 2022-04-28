namespace Uno.Extensions.Navigation;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseNavigation(
			this IHostBuilder builder,
			Action<IViewRegistry, IRouteRegistry>? viewRouteBuilder = null,
			Func<IServiceCollection, IViewRegistry>? createViewRegistry = null,
			Func<IServiceCollection, IRouteRegistry>? createRouteRegistry = null,
			Func<NavigationConfig, NavigationConfig>? configure = null)
	{
		return builder
			.ConfigureServices(sp =>
			{
				_ = sp.AddNavigation(configure, viewRouteBuilder, createViewRegistry, createRouteRegistry);
			});
	}
}
