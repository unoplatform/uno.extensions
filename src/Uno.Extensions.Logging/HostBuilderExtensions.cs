

namespace Uno.Extensions;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseLogging(
		this IHostBuilder hostBuilder,
		Action<ILoggingBuilder> configure, bool enableUnoLogging = false)
	{
		return hostBuilder.UseLogging((context, builder) => configure.Invoke(builder), enableUnoLogging);
	}

	public static IHostBuilder UseLogging(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, ILoggingBuilder>? configure = default, bool enableUnoLogging = false)
	{
		return hostBuilder
				.ConfigureLogging((context, builder) =>
				{
#if !__WASM__
#if __IOS__
#pragma warning disable CA1416 // Validate platform compatibility: The net6.0 version is not used on older versions of OS
					builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#pragma warning restore CA1416 // Validate platform compatibility
#else
					builder.AddConsole();
#endif
					builder.AddDebug();
#elif __WASM__         
                        builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#endif
					configure?.Invoke(context, builder);
				})
				.ConfigureServices(services =>
				{
					if (enableUnoLogging)
						services.AddSingleton<IServiceInitialize, LoggingInitializer>();
				});
	}

	public static IHost Build(
		this IHostBuilder hostBuilder,
		bool enableUnoLogging)
	{
		return hostBuilder
			.Build()
			.ConnectUnoLogging(enableUnoLogging);
	}

	private class LoggingInitializer : IServiceInitialize
	{
		private readonly IHost _host;
		public LoggingInitializer(IHost host) => _host = host;
		public void Initialize() =>
			_host.ConnectUnoLogging(true);
	}
}
