namespace Uno.Extensions;

/// <summary>
/// Extensions for working with <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Registers storage services.
	/// </summary>
	/// <param name="hostBuilder">The host builder instance to register with</param>
	/// <param name="configure">Callback for configuring services</param>
	/// <returns>The updated host builder instance</returns>
	public static IHostBuilder UseStorage(
		this IHostBuilder hostBuilder,
		Action<IServiceCollection> configure)
			=> hostBuilder.UseStorage((context, builder) => configure.Invoke(builder));

	/// <summary>
	/// Registers storage services
	/// </summary>
	/// <param name="hostBuilder">The host builder instance to register with</param>
	/// <param name="configure">Callback for configuring services</param>
	/// <returns></returns>
	public static IHostBuilder UseStorage(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection>? configure = default)
	{
		return hostBuilder
			.UseSerialization()
			.UseConfiguration(
				configure: configBuilder =>
				{
					if (configBuilder.IsRegistered(nameof(KeyValueStorageConfiguration)))
					{
						return configBuilder;
					}

					return configBuilder
							.Section<KeyValueStorageConfiguration>(nameof(KeyValueStorageConfiguration));
				})
			.ConfigureServices((ctx, services) =>
			{
				if (!ctx.IsRegistered(nameof(UseStorage)))
				{
					_ = services
						.AddFileStorage()
						.AddKeyedStorage();
				}
				configure?.Invoke(ctx, services);
			});
	}
}
