using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Logging
{
    public static class HostExtensions
    {
        public static IHost? EnableUnoLogging(this IHost host)
        {
            var factory = host?.Services?.GetService<ILoggerFactory>();
            if (factory is not null)
            {
                global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
            }
            return host;
        }
    }
}
