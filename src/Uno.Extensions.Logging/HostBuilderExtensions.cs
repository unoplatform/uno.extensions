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
#if !((NETSTANDARD || NET5_0 || NET6_0) && !__IOS__ && !__ANDROID__) || WINUI || __WASM__
    public static class HostBuilderExtensions
    {
#if !__WASM__
        public static IHostBuilder UseLogging(this IHostBuilder hostBuilder,
            Action<ILoggingBuilder>? configure = default)
        {
            return hostBuilder
                    .ConfigureLogging(builder =>
                    {
#if __IOS__
                        builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#elif NETFX_CORE
                        builder.AddDebug();
#else
                        builder.AddConsole();
#endif
                        configure?.Invoke(builder);
                    });
        }
#elif __WASM__
        public static IHostBuilder UseLogging(this IHostBuilder hostBuilder,
            Action<ILoggingBuilder>? configure = default)
        {
            return hostBuilder
                    .ConfigureLogging(builder =>
                    {
                        builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
                        configure?.Invoke(builder);
                    });
        }

#endif

		public static IHost Build(
			this IHostBuilder hostBuilder,
			bool enableUnoLogging)
		{
			return hostBuilder
				.Build()
				.ConnectUnoLogging(enableUnoLogging);
		}
    }
#endif
}
