

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
					if (!context.IsRegistered(nameof(UseLogging)))
					{
#if !__WASM__
#if __IOS__
#pragma warning disable CA1416 // Validate platform compatibility: The net7.0 version is not used on older versions of OS
						builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#pragma warning restore CA1416 // Validate platform compatibility
#elif NET6_0_OR_GREATER || __SKIA__ // Console isn't supported on all Xamarin targets, so only adding for net7.0 and above
						builder.AddConsole();
#endif
						builder.AddDebug();
#elif __WASM__
						builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#endif
					}

					configure?.Invoke(context, builder);
				})
				.ConfigureServices((ctx, services) =>
				{
					if (ctx.IsRegistered(nameof(HostExtensions.ConnectUnoLogging)))
					{
						return;
					}

					if (enableUnoLogging)
					{
						services.AddSingleton<IServiceInitialize, LoggingInitializer>();
					}
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

	private record LoggingInitializer(IHost Host) : IServiceInitialize
	{
		public void Initialize() =>
			Host.ConnectUnoLogging(true);
	}
}
