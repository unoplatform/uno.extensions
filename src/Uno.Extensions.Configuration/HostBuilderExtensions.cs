using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Uno.Extensions.Hosting;

namespace Uno.Extensions.Configuration
{
    public static class HostBuilderExtensions
    {
        public const string ConfigurationFolderName = "config";

        /// <summary>
        /// Makes the entire Configuration available to the app (required
        /// for use with IWritableOptions)
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseConfiguration(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                    .ConfigureServices((ctx, s) =>
                    {
                        s.TryAddSingleton<IConfiguration>(a => ctx.Configuration);
                        s.TryAddSingleton<IConfigurationRoot>(a => (IConfigurationRoot)ctx.Configuration);
                        s.TryAddSingleton<Reloader>();
                        _ = s.AddHostedService<ReloadService>();
                    });
        }

		public static IHostBuilder UseAppSettings(this IHostBuilder hostBuilder, bool includeEnvironmentSettings = true)
		{
			return hostBuilder
					.UseConfiguration()
					.ConfigureAppConfiguration((ctx, b) =>
					{
						b.AddAppSettings(ctx);
						if (includeEnvironmentSettings)
						{
							b.AddEnvironmentAppSettings(ctx);
						}
					});
		}

		public static IHostBuilder UseEmbeddedAppSettings<TApplicationRoot>(this IHostBuilder hostBuilder, bool includeEnvironmentSettings = true)
			where TApplicationRoot : class
		{
			return hostBuilder
					.UseConfiguration()
					.ConfigureAppConfiguration((ctx, b) =>
					{
						b.AddEmbeddedAppSettings<TApplicationRoot>();
						if (includeEnvironmentSettings)
						{
							b.AddEmbeddedEnvironmentAppSettings<TApplicationRoot>(ctx);
						}
					});
		}

        public static IHostBuilder UseSettings<TSettingsOptions>(
            this IHostBuilder hostBuilder,
            Func<HostBuilderContext, IConfigurationSection>? configSection = null)
                where TSettingsOptions : class, new()
        {
            if (configSection is null)
            {
                configSection = ctx => ctx.Configuration.GetSection(typeof(TSettingsOptions).Name);
            }

            static string FilePath(HostBuilderContext hctx)
            {
                var file = $"{ConfigurationFolderName}/{AppSettings.AppSettingsFileName}.{typeof(TSettingsOptions).Name}.json";
                var appData = (hctx.HostingEnvironment as IAppHostEnvironment)?.AppDataPath ?? string.Empty;
                var path = Path.Combine(appData, file);
                return path;
            }

            // This is consistent with HostBuilder, which defaults to Production
            return hostBuilder
                .UseConfiguration()
                .ConfigureAppConfiguration((ctx, b) =>
                    {
                        var path = FilePath(ctx);
                        Console.WriteLine($"iwrit Config path {path}");
                        b.AddJsonFile(path, optional: true, reloadOnChange: false); // In .NET6 we can enable this again because we can use polling
                    })
                    .ConfigureServices((ctx, services) =>
                    {
                        var section = configSection(ctx);
                        services.ConfigureAsWritable<TSettingsOptions>(section, FilePath(ctx));
                    }

                );
        }


        public static IHostBuilder UseConfigurationSectionInApp<TOptions>(this IHostBuilder hostBuilder, string? configurationSection = null)
            where TOptions : class
        {
            if (configurationSection is null)
            {
                configurationSection = typeof(TOptions).Name;
            }

            return hostBuilder
                .UseConfiguration()
                .ConfigureServices((ctx, services) => services.Configure<TOptions>(ctx.Configuration.GetSection(configurationSection)));
        }

        public static IHostBuilder AddConfigurationSectionFromEntity<TEntity>(
            this IHostBuilder hostBuilder,
            TEntity entity,
            string? sectionName = default)
        {
            return hostBuilder
                    .ConfigureHostConfiguration(
                        configurationBuilder => configurationBuilder.AddSectionFromEntity(entity, sectionName));
        }
    }
}
