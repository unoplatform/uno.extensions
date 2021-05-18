using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.EventLog;

namespace ApplicationTemplate.Hosting
{
    public static class UnoHost
    {
        public static IHostBuilder CreateDefaultBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureLogging((_, factory) =>
                {
//-:cnd:noEmit
#if WINDOWS_UWP  // We only need to do this on Windows for UWP because of an assumption dotnet makes that every Windows app can access eventlog
//+:cnd:noEmit
                    factory.Services.RemoveAllIncludeImplementations<EventLogLoggerProvider>();
//-:cnd:noEmit
#endif
//+:cnd:noEmit
                });
    }
}
