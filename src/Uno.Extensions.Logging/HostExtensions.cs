using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Logging
{
    public static class HostExtensions
    {
        public static IHost ConnectUnoLogging(this IHost host, bool enableUnoLogging = true)
        {
			if (!enableUnoLogging)
			{
				return host;
			}

            var factory = host.Services.GetRequiredService<ILoggerFactory>();
            if (factory is not null)
            {
                global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
				Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
			}
            return host;
        }
    }
}
