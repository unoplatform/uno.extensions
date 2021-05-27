using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Uno.Extensions.Logging
{
    public static class ServiceCollectionExtensions
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

        public static ILoggingBuilder MinimumLogLevel(this ILoggingBuilder builder, LogLevel minLogLevel)
        {
            // Exclude logs below this level
            builder.SetMinimumLevel(minLogLevel);
            return builder;
        }

        public static ILoggingBuilder CoreLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
        {
            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", logLevel);
            builder.AddFilter("Windows", logLevel);
            builder.AddFilter("Microsoft", logLevel);
            return builder;
        }

        public static ILoggingBuilder XamlLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
        {
            // Generic Xaml events
            builder.AddFilter("Microsoft.UI.Xaml", logLevel);
            builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", logLevel);
            builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", logLevel);
            builder.AddFilter("Microsoft.UI.Xaml.UIElement", logLevel);
            builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", logLevel);
            return builder;
        }

        public static ILoggingBuilder XamlLayoutLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
        {
            // Layouter specific messages
            builder.AddFilter("Microsoft.UI.Xaml.Controls", logLevel);
            builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", logLevel);
            builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", logLevel);
            return builder;
        }

        public static ILoggingBuilder StorageLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
        {
            builder.AddFilter("Windows.Storage", logLevel);
            return builder;
        }

        public static ILoggingBuilder XamlBindingLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
        {
            // Binding related messages
            builder.AddFilter("Microsoft.UI.Xaml.Data", logLevel);
            builder.AddFilter("Microsoft.UI.Xaml.Data", logLevel);
            return builder;
        }

        public static ILoggingBuilder BinderMemoryReferenceLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
        {
            // Binder memory references tracking
            builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", logLevel);
            return builder;
        }

        public static ILoggingBuilder HotReloadCoreLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
        {
            // RemoteControl and HotReload related
            builder.AddFilter("Uno.UI.RemoteControl", logLevel);
            return builder;
        }

        public static ILoggingBuilder WebAssemblyLogLevel(this ILoggingBuilder builder, LogLevel logLevel)
        {
            // Debug JS interop
            builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", logLevel);
            return builder;
        }
    }
}
