

namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IHostBuilder"/> to configure logging.
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Configures logging with the default configuration.
	/// </summary>
	/// <param name="hostBuilder">
	/// The <see cref="IHostBuilder"/> to configure.
	/// </param>
	/// <param name="configure">
	/// Used to provide additional configuration for the logger.
	/// </param>
	/// <param name="enableUnoLogging">
	/// Whether to enable Uno logging. Optional.
	/// </param>
	/// <returns>
	/// The <see cref="IHostBuilder"/> that was passed in.
	/// </returns>
	public static IHostBuilder UseLogging(
		this IHostBuilder hostBuilder,
		Action<ILoggingBuilder> configure, bool enableUnoLogging = false)
	{
		return hostBuilder.UseLogging((context, builder) => configure.Invoke(builder), enableUnoLogging);
	}

	/// <summary>
	/// Configures logging with the default configuration.
	/// </summary>
	/// <param name="hostBuilder">
	/// The <see cref="IHostBuilder"/> to configure.
	/// </param>
	/// <param name="configure">
	/// Used to provide additional configuration for the logger. Optional.
	/// </param>
	/// <param name="enableUnoLogging">
	/// Whether to enable Uno logging. Optional.
	/// </param>
	/// <returns>
	/// The <see cref="IHostBuilder"/> that was passed in.
	/// </returns>
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

	/// <summary>
	/// Builds the host and configures logging with the default configuration.
	/// </summary>
	/// <param name="hostBuilder">
	/// The <see cref="IHostBuilder"/> to configure.
	/// </param>
	/// <param name="enableUnoLogging">
	/// Whether to enable Uno internal logging.
	/// </param>
	/// <returns></returns>
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
