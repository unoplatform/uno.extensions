namespace Uno.Extensions.Logging;

/// <summary>
/// Extensions for <see cref="IHost"/> to customize logging behavior.
/// </summary>
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
#if HAS_UNO
			global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

			Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();

			// The ambient logger factory is a process-wide static that otherwise keeps this host's
			// ILoggerFactory — and its whole ServiceProvider — alive after the host stops. For a downstream
			// host that loads previewed apps into their own collectible AssemblyLoadContexts, that pins the
			// app's ALC forever. Reset it (only if still ours) when the application stops.
			if (host.Services.GetService<IHostApplicationLifetime>() is { } lifetime)
			{
				lifetime.ApplicationStopping.Register(() =>
				{
					if (ReferenceEquals(global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory, factory))
					{
						global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
					}
				});
			}
#endif
		}
		return host;
	}
}
