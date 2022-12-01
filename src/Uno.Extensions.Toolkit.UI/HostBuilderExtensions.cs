namespace Uno.Extensions
{
	public static class HostBuilderExtensions
	{
		public static IHostBuilder UseThemeSwitching(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection>? configure = default)
		{
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
}
