namespace Uno.Extensions.Logging;

public static class HostExtensions
{
	internal static IHost ConnectUnoLogging(this IHost host, bool enableUnoLogging = true)
	{
		if (!enableUnoLogging)
		{
			return host;
		}

		var factory = host.Services.GetRequiredService<ILoggerFactory>();
		if (factory is not null)
		{
#if HAS_UNO
			global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

			Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
		}
		return host;
	}
}
