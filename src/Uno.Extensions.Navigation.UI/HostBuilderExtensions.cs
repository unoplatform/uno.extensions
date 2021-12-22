namespace Uno.Extensions.Navigation;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseNavigation(
			this IHostBuilder builder,
			Action<IRouteRegistry>? routeBuilder = null)
	{
		return builder
			.ConfigureServices(sp =>
			{
				_ = sp.AddNavigation(routeBuilder);
			});
	}
}
