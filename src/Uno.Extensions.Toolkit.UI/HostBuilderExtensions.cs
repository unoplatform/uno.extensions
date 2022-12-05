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
			.UseConfiguration(
				configure: configBuilder =>
					configBuilder
						.Section<ThemeSettings>()
					)

			.ConfigureServices((ctx, services) =>
			{
				_ = services
				.AddScoped<IThemeService, ThemeService>();
			});
	}
}
