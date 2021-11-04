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
        public static IHostBuilder UseHostConfigurationForApp(this IHostBuilder hostBuilder)
        {
            return hostBuilder?
                    .ConfigureServices((ctx, s) =>
                    {
                        _ = s.AddSingleton<IConfiguration>(a => ctx.Configuration)
                            .AddSingleton<IConfigurationRoot>(a => ctx.Configuration as IConfigurationRoot);
                    }
            );
//                    .ConfigureServices((ctx, services) =>
//                                 {
//                services.TryAddSingleton<Reloader>();
//#if NETSTANDARD || __WASM__
//                                     _ = services
//                                             .AddHostedService<ReloadService>();
//#endif
//            });
            //;
        }

        public static IHostBuilder UseEmbeddedAppSettings<TApplicationRoot>(this IHostBuilder hostBuilder)
            where TApplicationRoot : class
        {
            return hostBuilder
                    .ConfigureAppConfiguration(b =>
                        b.AddEmbeddedAppSettings<TApplicationRoot>()
                    );
        }

        public static IHostBuilder UseEmbeddedEnvironmentAppSettings<TApplicationRoot>(this IHostBuilder hostBuilder)
            where TApplicationRoot : class
        {
            // This is consistent with HostBuilder, which defaults to Production
            return hostBuilder?
                    .ConfigureAppConfiguration((ctx, b) =>
                        b.AddEmbeddedEnvironmentAppSettings<TApplicationRoot>(ctx.HostingEnvironment?.EnvironmentName ?? Environments.Production));
        }

        public static IHostBuilder UseWritableSettings<TSettingsOptions>(
            this IHostBuilder hostBuilder,
            Func<HostBuilderContext, IConfigurationSection> configSection)
                where TSettingsOptions : class, new()
        {
            static string FilePath(HostBuilderContext hctx)
            {
                var file = $"{ConfigurationFolderName}/{AppSettings.AppSettingsFileName}.{typeof(TSettingsOptions).Name}.json";
                var appData = (hctx.HostingEnvironment as IAppHostEnvironment).AppDataPath;
                var path = Path.Combine(appData, file);
                //var fileProvider = hctx.HostingEnvironment.ContentRootFileProvider;
                //var fileInfo = fileProvider.GetFileInfo(file);
                //return fileInfo.PhysicalPath;
                return path;
            }

            // This is consistent with HostBuilder, which defaults to Production
            return hostBuilder?
                .ConfigureAppConfiguration((ctx, b) =>
                    {
                        var path = FilePath(ctx);
                        Console.WriteLine($"iwrit Config path {path}");
                        b.AddJsonFile(path, optional: true, reloadOnChange: false); // In .NET6 we can enable this again because we can use polling
                    })
                    .ConfigureServices((ctx, services) =>
                    {
                        var section = configSection(ctx);
                        services.TryAddSingleton<Reloader>();
                        _ = services
#if NETSTANDARD || __WASM__
                                .AddHostedService<ReloadService>()
#endif
                                .ConfigureAsWritable<TSettingsOptions>(section, FilePath(ctx));
                    }

                );
        }


        public static IHostBuilder UseConfigurationSectionInApp<TOptions>(this IHostBuilder hostBuilder, string configurationSection)
            where TOptions : class
        {
            return hostBuilder?
                .ConfigureServices((ctx, services) => services.Configure<TOptions>(ctx.Configuration.GetSection(configurationSection)));
        }

        public static IHostBuilder AddConfigurationSectionFromEntity<TEntity>(
            this IHostBuilder hostBuilder,
            TEntity entity,
            string sectionName = null)
        {
            return hostBuilder?
                    .ConfigureHostConfiguration(
                        configurationBuilder => configurationBuilder.AddSectionFromEntity(entity, sectionName));
        }
    }
}
