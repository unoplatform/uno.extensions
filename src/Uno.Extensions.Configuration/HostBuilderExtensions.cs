namespace Uno.Extensions;

/// <summary>
/// Extension methods for setting up <see cref="IHostBuilder"/> to use configuration.
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Enables configuration to be set up for both the builder and application.
	/// </summary>
	/// <param name="hostBuilder">
	/// The <see cref="IHostBuilder"/> to configure.
	/// </param>
	/// <param name="configureHostConfiguration">
	/// Used to configure the <see cref="IConfigurationBuilder"/> for the builder itself.
	/// </param>
	/// <param name="configureAppConfiguration">
	/// Used to configure the <see cref="IConfigurationBuilder"/> for the application.
	/// </param>
	/// <param name="configure">
	/// Allows for adding additional configuration sources to the <see cref="IConfigBuilder"/>.
	/// </param>
	/// <returns>
	/// The same instance of the <see cref="IHostBuilder"/> for chaining.
	/// </returns>
	public static IHostBuilder UseConfiguration(
		this IHostBuilder hostBuilder,
		Action<IConfigurationBuilder>? configureHostConfiguration = default,
		Action<HostBuilderContext, IConfigurationBuilder>? configureAppConfiguration = default,
		Func<IConfigBuilder, IHostBuilder>? configure = default)
	{
		if (configureHostConfiguration is not null)
		{
			hostBuilder = hostBuilder.ConfigureHostConfiguration(configureHostConfiguration);
		}

		if (configureAppConfiguration is not null)
		{
			hostBuilder = hostBuilder.ConfigureAppConfiguration(configureAppConfiguration);
		}

		hostBuilder = hostBuilder.UseSerialization()
			.ConfigureServices((ctx, s) =>
				{
					// We're doing the IsRegistered check here so that the
					// other configure delegates still run if required
					if (ctx.IsRegistered(nameof(UseConfiguration)))
					{
						return;
					}

					s.TryAddSingleton(a => ctx.Configuration);
					s.TryAddSingleton(a => (IConfigurationRoot)ctx.Configuration);
					s.TryAddSingleton<Reloader>();
					s.TryAddSingleton<ReloadService>();
					_ = s.AddHostedService(sp => sp.GetRequiredService<ReloadService>());
					s.TryAddSingleton<IStartupService>(sp => sp.GetRequiredService<ReloadService>());
				});
		hostBuilder = configure?.Invoke(hostBuilder.AsConfigBuilder()) ?? hostBuilder;

		return hostBuilder;
	}


	internal static IConfigBuilder AsConfigBuilder(this IHostBuilder hostBuilder)
	{
		if (hostBuilder is ConfigBuilder configBuilder)
		{
			return configBuilder;
		}

		return new ConfigBuilder(hostBuilder);
	}
}
