using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IHostBuilder"/> to register toolkit services
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Registers toolkit services with the host builder
	/// </summary>
	/// <param name="hostBuilder">The host builder to register with</param>
	/// <returns></returns>
	public static IHostBuilder UseToolkit(
		this IHostBuilder hostBuilder)
			=> hostBuilder.UseThemeSwitching();

	/// <summary>
	/// Registers theme switching services with the host builder
	/// </summary>
	/// <param name="hostBuilder">The host builder to register with</param>
	/// <returns>The updated host builder</returns>
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
				// ThemeService persists through ISettings. UseStorage registers the same
				// implementation; TryAdd so whichever runs first wins and an app's own stays.
				services.TryAddSingleton<ISettings, Settings>();
				_ = services.AddScoped<IThemeService, ScopedThemeService>();
			});
	}
}
