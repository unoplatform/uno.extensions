using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.EventLog;

namespace Uno.Extensions.Hosting
{
    public static class UnoHost
    {
        public static IHostBuilder CreateDefaultBuilder() =>
            Host.CreateDefaultBuilder()
            .UseContentRoot(PlatformSpecificContentRootPath())
#if __IOS__
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
#if WINDOWS_UWP  // We only need to do this on Windows for UWP because of an assumption dotnet makes that every Windows app can access eventlog
                    factory.Services.RemoveAllIncludeImplementations<EventLogLoggerProvider>();
#endif
                })
#if __ANDROID__ || __IOS__
            .ConfigureServices(services =>
            {
                services.AddSingleton<IHostLifetime, XamarinConsoleLifetime>();
            })
#endif
            ;

        private static string PlatformSpecificContentRootPath()
        {
#if WINUI || WINDOWS_UWP
            return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#elif __ANDROID__ || __IOS__
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#else
            return string.Empty;
#endif
        }
    }
}
