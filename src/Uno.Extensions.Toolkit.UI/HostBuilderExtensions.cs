namespace Uno.Extensions;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseToolkit(
		this IHostBuilder hostBuilder)
	{
		return hostBuilder.UseThemeSwitching();
	}

	private static bool _didRegisterThemeSwitching;

	public static IHostBuilder UseThemeSwitching(
		this IHostBuilder hostBuilder)
	{
		if (_didRegisterThemeSwitching)
		{
			return hostBuilder;
		}

		_didRegisterThemeSwitching = true;

		return hostBuilder
			.ConfigureServices((ctx, services) =>
			{
				_ = services
				.AddScoped<IThemeService, ScopedThemeService>();
			});
	}
}
