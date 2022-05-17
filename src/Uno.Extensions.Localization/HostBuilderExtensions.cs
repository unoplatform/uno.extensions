using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Uno.Extensions.Configuration;

namespace Uno.Extensions.Localization;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseLocalization(
		this IHostBuilder hostBuilder,
		Action<IServiceCollection> configure)
	{
		return hostBuilder.UseConfiguration((context, builder) => configure.Invoke(builder));
	}

	public static IHostBuilder UseLocalization(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection>? configure = default)
	{
		return hostBuilder
			.UseSettings<LocalizationSettings>(ctx => ctx.Configuration.GetSection(nameof(LocalizationSettings)))

			.ConfigureServices((ctx, services) =>
		{
			_ = services
			.AddHostedService<LocalizationService>()
			.AddSingleton<IStringLocalizer, ResourceLoaderStringLocalizer>();
		});
	}
}
