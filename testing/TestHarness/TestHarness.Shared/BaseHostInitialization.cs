namespace TestHarness;

public abstract class BaseHostInitialization : IHostInitialization
{
	protected virtual string[] ConfigurationFiles => Array.Empty<string>();

	public virtual IHost InitializeHost()
	{
		return UnoHost
				.CreateDefaultBuilder()

				.Use(builder => Environment(builder))

				.Use(builder => Configuration(builder))

				.Use(builder => Logging(builder))

				.Use(builder => Navigation(builder))

				.Use(builder => Custom(builder))

				.Use(builder => Serialization(builder))

				.Use(builder => Localization(builder))

				.Build(enableUnoLogging: true);
	}

	protected virtual IHostBuilder Localization(IHostBuilder builder)
	{
		return builder.UseLocalization();
	}

	protected virtual IHostBuilder Serialization(IHostBuilder builder)
	{
		return builder.UseSerialization();
	}

	protected virtual IHostBuilder Custom(IHostBuilder builder)
	{
		return builder;
	}

	protected virtual IHostBuilder Environment(IHostBuilder builder)
	{
		return builder
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif
				;
	}

	protected virtual IHostBuilder Navigation(IHostBuilder builder)
	{
		return builder
				.UseNavigation(RegisterRoutes)
				.UseToolkitNavigation();
	}

	protected virtual IHostBuilder Configuration(IHostBuilder builder)
	{
		builder = builder
				.UseConfiguration();
		foreach (var file in ConfigurationFiles)
		{
			// Only use this syntax for UI tests - use UseConfiguration in apps
			builder = builder.ConfigureAppConfiguration((ctx, b) =>
			{
				b.AddEmbeddedConfigurationFile<App>(file);
			});

		}
		return builder; ;
	}

	protected virtual IHostBuilder Logging(IHostBuilder builder)
	{
		return builder
				.UseSerilog(true, true)

				// Add platform specific log providers
				.UseLogging(configure: (context, logBuilder) =>
				{
					var host = context.HostingEnvironment;
					// Configure log levels for different categories of logging
					logBuilder.SetMinimumLevel(host.IsDevelopment() ? LogLevel.Information : LogLevel.Warning);
				});
	}


	protected virtual void RegisterRoutes(IViewRegistry views, IRouteRegistry routes) { }
}
