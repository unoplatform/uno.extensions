using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.EventLog;

namespace Uno.Extensions.Hosting;

#if !NETSTANDARD || WINUI || __WASM__
public static class UnoHost
{
    public static IHostBuilder CreateDefaultBuilder(bool custom = true) =>
        (custom ? CustomHost.CreateDefaultBuilder() : Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder())
        .ConfigureAppConfiguration((ctx, appConfig) =>
        {
            var appHost = AppHostingEnvironment.FromHostEnvironment(ctx.HostingEnvironment, PlatformSpecificContentRootPath());
            ctx.HostingEnvironment = appHost;
        })
        .ConfigureServices((ctx, services) =>
        {
            var appHost = ctx.HostingEnvironment as IAppHostEnvironment;
            //var appHost = AppHostingEnvironment.FromHostEnvironment(ctx.HostingEnvironment, PlatformSpecificContentRootPath());
            services.AddSingleton<IAppHostEnvironment>(appHost);
        })
            //#if WINUI || WINDOWS_UWP || __IOS__ || __ANDROID__ || NETSTANDARD
            //            .UseContentRoot(PlatformSpecificContentRootPath())
            //#endif
#if __IOS__ || NETSTANDARD
            .ConfigureHostConfiguration(config =>
            {
                var disablereload = new Dictionary<string, string>
                        {
                            { "hostBuilder:reloadConfigOnChange", "false" },
                        };
                config.AddInMemoryCollection(disablereload);
            })
#endif
#if __WASM__
            .ConfigureHostConfiguration(config =>
            {
                // Note that this environment variable is being set so that in .net6 we can leverage polling file watcher
                Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
                var appsettingsPrefix = new Dictionary<string, string>
                        {
                            { "appsettings:prefix", "local" },
                        };
                config.AddInMemoryCollection(appsettingsPrefix);
            })
#endif
            .ConfigureLogging((_, factory) =>
            {
#if WINDOWS_UWP || NETSTANDARD // We only need to do this on Windows for UWP because of an assumption dotnet makes that every Windows app can access eventlog
                factory.Services.RemoveAllIncludeImplementations<EventLogLoggerProvider>();
                factory.Services.RemoveWhere(sd => sd?.ImplementationType?.Name == "EventLogFiltersConfigureOptions");
                factory.Services.RemoveWhere(sd => sd?.ImplementationType?.Name == "EventLogFiltersConfigureOptionsChangeSource");
#endif
#if __WASM__
                factory.Services.RemoveAllIncludeImplementations<ConsoleLoggerProvider>();
#endif
            })
#if __ANDROID__ || __IOS__ || NETSTANDARD
            .ConfigureServices(services =>
            {
                if (!custom)
                {
                    services.AddSingleton<IHostLifetime, XamarinConsoleLifetime>();
                }
            })
#endif
            ;

    private static string PlatformSpecificContentRootPath()
    {
#if WINUI || WINDOWS_UWP || NETSTANDARD
            return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#elif __ANDROID__ || __IOS__
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#else
            return null;
#endif
    }
}

#endif
