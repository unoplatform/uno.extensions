namespace Uno.Extensions;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseToolkit(
		this IHostBuilder hostBuilder)
	{
		return hostBuilder.UseThemeSwitching();
	}

	public static IHostBuilder UseThemeSwitching(
		this IHostBuilder hostBuilder)
	{
		if (hostBuilder.IsRegistered(nameof(UseThemeSwitching)))
		{
			return hostBuilder;
		}

		return hostBuilder
			.ConfigureServices((ctx, services) =>
			{
				_ = services
				.AddScoped<IThemeService, ScopedThemeService>();
			});
	}
}
