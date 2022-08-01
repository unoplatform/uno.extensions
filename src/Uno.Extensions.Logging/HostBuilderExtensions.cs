

namespace Uno.Extensions;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseLogging(
		this IHostBuilder hostBuilder,
		Action<ILoggingBuilder> configure)
	{
		return hostBuilder.UseLogging((context, builder) => configure.Invoke(builder));
	}
	public static IHostBuilder UseLogging(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, ILoggingBuilder>? configure = default)
	{
		return hostBuilder
				.ConfigureLogging((context, builder) =>
				{
#if !__WASM__
#if __IOS__
                        builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#else
						builder.AddDebug();
#endif
#elif __WASM__
                        builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#endif
					configure?.Invoke(context, builder);
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
}
