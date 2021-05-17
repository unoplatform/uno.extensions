using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Logging.EventLog;

namespace ApplicationTemplate.Hosting
{
    public static class UnoHost
    {
        public static IHost CreateDefaultHostWithStartup<TStartup>(TStartup? configurer = default)
            where TStartup : IServiceConfigurer, new()
        {
            configurer = configurer ?? new TStartup();
            return CreateDefaultHost(configurer);
        }
            public static IHost CreateDefaultHost(IServiceConfigurer configurer=null)
        {
            var builder = Host.CreateDefaultBuilder()
               .ConfigureHostConfiguration(configBuilder =>
               {
                   configBuilder
                   //.AddCommandLine(args)
                       .AddEnvironmentVariables(prefix: "DOTNET_");
               })
               .ConfigureLogging((_, factory) =>
               {
                   factory.AddConsole();
                   factory.AddFilter<ConsoleLoggerProvider>(level => level >= LogLevel.Warning);
               })
               .ConfigureServices((_, services) =>
               {
//-:cnd:noEmit
#if WINDOWS_UWP  // We only need to do this on Windows for UWP because of an assumption dotnet makes that every Windows app can access eventlog
//+:cnd:noEmit
                   services.RemoveAllIncludeImplementations<EventLogLoggerProvider>();
//-:cnd:noEmit
#endif
//+:cnd:noEmit
                   configurer?.ConfigureServices(services);
               });
               return builder.Build();


            //    if (config["STARTMECHANIC"] == "Run")
            //    {
            //        host.Run();
            //    }
            //    else if (config["STARTMECHANIC"] == "WaitForShutdown")
            //    {
            //        host.Start();
            //        host.WaitForShutdown();
            //    }
            //    else
            //    {
            //        throw new InvalidOperationException("Starting mechanic not specified");
            //    }
            //}
        }
    }
}


namespace Microsoft.Extensions.DependencyInjection.Extensions
{
    public static class ServiceCollectionDescriptorExtensions
    {
        public static IServiceCollection RemoveAllIncludeImplementations<T>(this IServiceCollection collection)
        {
            return RemoveAllIncludeImplementations(collection, typeof(T));
        }

        public static IServiceCollection RemoveAllIncludeImplementations(this IServiceCollection collection, Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            for (int i = collection.Count - 1; i >= 0; i--)
            {
                ServiceDescriptor? descriptor = collection[i];
                if (descriptor.ServiceType == serviceType || descriptor.ImplementationType == serviceType)
                {
                    collection.RemoveAt(i);
                }
            }

            return collection;
        }
    }
}
