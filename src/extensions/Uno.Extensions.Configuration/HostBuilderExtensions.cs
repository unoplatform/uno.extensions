using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Uno.Extensions.Configuration
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseHostConfigurationForApp(this IHostBuilder hostBuilder)
        {
            return hostBuilder?
                    .ConfigureServices((ctx, s) =>
                    {
                        _ = s.AddSingleton<IConfiguration>(a => ctx.Configuration)
                            .AddSingleton<IConfigurationRoot>(a => ctx.Configuration as IConfigurationRoot);
                    }
            );
        }

        public static IHostBuilder UseAppSettingsForHostConfiguration<TApplicationRoot>(this IHostBuilder hostBuilder)
            where TApplicationRoot : class
        {
            return hostBuilder?
                    .ConfigureHostConfiguration(b =>
                        b.AddAppSettings<TApplicationRoot>()
                    );
        }

        public static IHostBuilder UseAppSettings<TApplicationRoot>(this IHostBuilder hostBuilder)
            where TApplicationRoot : class
        {
            return hostBuilder
                    .ConfigureAppConfiguration(b =>
                        b.AddAppSettings<TApplicationRoot>()
                    );
        }

        public static IHostBuilder UseEnvironmentAppSettings<TApplicationRoot>(this IHostBuilder hostBuilder)
            where TApplicationRoot : class
        {
            // This is consistent with HostBuilder, which defaults to Production
            return hostBuilder?
                    .ConfigureAppConfiguration((ctx, b) =>
                        b.AddEnvironmentAppSettings<TApplicationRoot>(ctx.HostingEnvironment?.EnvironmentName ?? Environments.Production));
        }

        public static IHostBuilder UseWritableSettings<TSettingsOptions>(
            this IHostBuilder hostBuilder,
            Func<HostBuilderContext, IConfigurationSection> configSection)
                where TSettingsOptions : class, new()
        {
            static string FilePath(HostBuilderContext hctx)
            {
                var file = $"{AppSettings.AppSettingsFileName}.{typeof(TSettingsOptions).Name}.json";

                var fileProvider = hctx.HostingEnvironment.ContentRootFileProvider;
                var fileInfo = fileProvider.GetFileInfo(file);
                return fileInfo.PhysicalPath;
            }

            // This is consistent with HostBuilder, which defaults to Production
            return hostBuilder?
                .ConfigureAppConfiguration((ctx, b) =>
                    {
                        b.AddJsonFile(FilePath(ctx), optional: true, reloadOnChange: true);
                    })
                    .ConfigureServices((ctx, services) =>
                    {
                        var section = configSection(ctx);
                        _ = services.ConfigureAsWritable<TSettingsOptions>(section, FilePath(ctx));
                    }
                );
        }

        public static IHostBuilder UseConfigurationSectionInApp<TOptions>(this IHostBuilder hostBuilder, string configurationSection)
            where TOptions : class
        {
            return hostBuilder?
                .ConfigureServices((ctx, services) => services.Configure<TOptions>(ctx.Configuration.GetSection(configurationSection)));
        }
    }
}
