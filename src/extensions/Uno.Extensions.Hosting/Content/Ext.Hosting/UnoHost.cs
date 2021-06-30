using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.EventLog;

namespace Uno.Extensions.Hosting
{
    public static class UnoHost
    {
        public static IHostBuilder CreateDefaultBuilderForWASM() =>
            CreateDefaultBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    // Note that this environment variable is being set so that in .net6 we can leverage polling file watcher
                    Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true"); 
                })
                .ConfigureLogging((_, factory) =>
                {
                    factory.Services.RemoveAllIncludeImplementations<ConsoleLoggerProvider>();
                });
        

        public static IHostBuilder CreateDefaultBuilder() =>
            Host.CreateDefaultBuilder()
#if WINUI || WINDOWS_UWP || __IOS__ || __ANDROID__ || NETSTANDARD
            .UseContentRoot(PlatformSpecificContentRootPath())
#endif
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
            .ConfigureLogging((_, factory) =>
            {
#if WINDOWS_UWP || NETSTANDARD // We only need to do this on Windows for UWP because of an assumption dotnet makes that every Windows app can access eventlog
                factory.Services.RemoveAllIncludeImplementations<EventLogLoggerProvider>();
                factory.Services.RemoveWhere(sd => sd?.ImplementationType?.Name == "EventLogFiltersConfigureOptions");
                factory.Services.RemoveWhere(sd => sd?.ImplementationType?.Name == "EventLogFiltersConfigureOptionsChangeSource");
#endif
            })
#if __ANDROID__ || __IOS__  || NETSTANDARD
            .ConfigureServices(services =>
            {
                services.AddSingleton<IHostLifetime, XamarinConsoleLifetime>();
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
            return string.Empty;
#endif
        }
    }
}
