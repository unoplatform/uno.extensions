namespace Uno.Extensions.Navigation;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseNavigation(
			this IHostBuilder builder,
			Action<IViewRegistry, IRouteRegistry>? viewRouteBuilder = null)
	{
		return builder
			.ConfigureServices(sp =>
			{
				_ = sp.AddNavigation(viewRouteBuilder);
			});
	}
}
