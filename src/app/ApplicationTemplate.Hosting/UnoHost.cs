using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

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
            var builder = new HostBuilder()
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
