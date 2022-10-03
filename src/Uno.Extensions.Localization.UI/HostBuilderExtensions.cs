using Uno.Extensions.Hosting;

namespace Uno.Extensions.Localization;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseLocalization(
		this IHostBuilder hostBuilder,
		Action<IServiceCollection> configure)
	{
		return hostBuilder.UseLocalization((context, builder) => configure.Invoke(builder));
	}

	public static IHostBuilder UseLocalization(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection>? configure = default)
	{
		return hostBuilder
			.UseConfiguration(
				configure: configBuilder =>
					configBuilder
						.Section<LocalizationConfiguration>(nameof(LocalizationConfiguration))
						.Section<LocalizationSettings>(nameof(LocalizationSettings))
					)

			.ConfigureServices((ctx, services) =>
		{
			_ = services
			.AddSingleton<LocalizationService>()
			.AddSingleton<IServiceInitialize>(sp => sp.GetRequiredService<LocalizationService>())
			.AddSingleton<ILocalizationService>(sp => sp.GetRequiredService<LocalizationService>())
			.AddSingleton<IStringLocalizer, ResourceLoaderStringLocalizer>();
		});
	}
}
