namespace Uno.Extensions.Toolkit.UI
{
	public static class HostBuilderExtensions
	{
		public static IHostBuilder UseThemeSwitching(
		this IHostBuilder hostBuilder)
		{
			return
			 hostBuilder
			.UseConfiguration(configure: configBuilder =>
				configBuilder
					.Section<ThemeSettings>()
			);
		}
	}
}
