using System;
using Microsoft.Extensions.Hosting;

namespace Uno.Extensions.Navigation;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseNavigation(
			this IHostBuilder builder,
			Action<IRouteBuilder>? routeBuilder = null)
	{
		return builder
			.ConfigureServices(sp =>
			{
				_ = sp.AddNavigation(routeBuilder);
			});
	}
}
