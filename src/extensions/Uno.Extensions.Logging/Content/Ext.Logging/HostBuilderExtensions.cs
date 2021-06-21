using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Uno.Extensions.Logging
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseUnoLogging(this IHostBuilder hostBuilder,
            Action<ILoggingBuilder> configure = null,
            ILoggerProvider consoleProvider = null)
        {
            return hostBuilder
                    .ConfigureLogging(builder =>
                        {
                            if (consoleProvider == null)
                            {
#if __IOS__
                                builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#elif NETFX_CORE
                                builder.AddDebug();
#else
                                builder.AddConsole();
#endif
                            }
                            else
                            {
                                builder.AddProvider(consoleProvider);
                            }
                            configure?.Invoke(builder);
                        });
        }
    }
}
