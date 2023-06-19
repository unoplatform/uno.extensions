namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IHostBuilder"/>
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Registers the native HttpMessageHandler and the DiagnosticHandler
	/// </summary>
	/// <param name="builder">The host builder to register with</param>
	/// <param name="configure">Callback for configuring Http services</param>
	/// <returns>Updated host builder</returns>
	public static IHostBuilder UseHttp(
		this IHostBuilder builder,
		Action<IServiceCollection> configure)
	{
		return builder.UseHttp((context, builder) => configure.Invoke(builder));
	}

	/// <summary>
	/// Registers the native HttpMessageHandler and the DiagnosticHandler
	/// </summary>
	/// <param name="builder">The host builder to register with</param>
	/// <param name="configure">Callback for configuring Http services</param>
	/// <returns>Updated host builder</returns>
	public static IHostBuilder UseHttp(
		this IHostBuilder builder,
		Action<HostBuilderContext, IServiceCollection>? configure = default)
	{
		return builder
			.ConfigureServices((ctx, services) =>
		{
			if (!ctx.IsRegistered(nameof(UseHttp)))
			{
				_ = services
					.AddCookieManager(ctx)
					.AddNativeHandler(ctx)
					.AddTransient<DelegatingHandler, DiagnosticHandler>();
			}

			configure?.Invoke(ctx, services);
		});
	}
}
