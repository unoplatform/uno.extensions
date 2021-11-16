using System;
using Microsoft.Extensions.Hosting;

namespace Uno.Extensions.Navigation.Toolkit;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseToolkitNavigation(
			this IHostBuilder builder)
	{
		return builder
			.ConfigureServices(sp =>
			{
				_ = sp.AddToolkitNavigation();
			});
	}
}
