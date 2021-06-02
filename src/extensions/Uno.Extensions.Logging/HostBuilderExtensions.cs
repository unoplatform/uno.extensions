using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Uno.Extensions.Logging
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseUnoLogging(this IHostBuilder hostBuilder)
        {
            return hostBuilder.UseUnoLogging(builder => { });
        }

        public static IHostBuilder UseUnoLogging(this IHostBuilder hostBuilder,
            Action<ILoggingBuilder> configure)
        {
            var factory = LoggerFactory.Create(builder =>
            {
#if __WASM__
                builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
                builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#elif NETFX_CORE
                builder.AddDebug();
#else
                builder.AddConsole();
#endif
                configure(builder);
            });

            global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
            return hostBuilder;
        }
    }
}
