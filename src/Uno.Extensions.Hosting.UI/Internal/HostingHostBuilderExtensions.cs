namespace Uno.Extensions.Hosting;

public static class HostingHostBuilderExtensions
{
	/// <summary>
	/// Specify the content root directory to be used by the host. To avoid the content root directory being
	/// overwritten by a default value, ensure this is called after defaults are configured.
	/// </summary>
	/// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
	/// <param name="contentRoot">Path to root directory of the application.</param>
	/// <returns>The <see cref="IHostBuilder"/>.</returns>
	public static IHostBuilder UseContentRoot(this IHostBuilder hostBuilder, string contentRoot)
	{
		return hostBuilder.ConfigureHostConfiguration(configBuilder =>
		{
			configBuilder.AddInMemoryCollection(new[]
			{
					new KeyValuePair<string, string?>(HostDefaults.ContentRootKey,
						contentRoot ?? throw new ArgumentNullException(nameof(contentRoot)))
			});
		});
	}


	/// <summary>
	/// Configures an existing <see cref="IHostBuilder"/> instance with pre-configured defaults. This will overwrite
	/// previously configured values and is intended to be called before additional configuration calls.
	/// </summary>
	/// <remarks>
	///   The following defaults are applied to the <see cref="IHostBuilder"/>:
	///   <list type="bullet">
	///     <item><description>set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/></description></item>
	///     <item><description>load host <see cref="IConfiguration"/> from "DOTNET_" prefixed environment variables</description></item>
	///     <item><description>load host <see cref="IConfiguration"/> from supplied command line args</description></item>
	///     <item><description>load app <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json'</description></item>
	///     <item><description>load app <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly</description></item>
	///     <item><description>load app <see cref="IConfiguration"/> from environment variables</description></item>
	///     <item><description>load app <see cref="IConfiguration"/> from supplied command line args</description></item>
	///     <item><description>configure the <see cref="ILoggerFactory"/> to log to the console, debug, and event source output</description></item>
	///     <item><description>enables scope validation on the dependency injection container when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development'</description></item>
	///   </list>
	/// </remarks>
	/// <param name="builder">The existing builder to configure.</param>
	/// <param name="args">The command line args.</param>
	/// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
	public static IHostBuilder ConfigureCustomDefaults(this IHostBuilder builder, string[]? args)
	{
		UseContentRoot(builder, Directory.GetCurrentDirectory());
		builder.ConfigureHostConfiguration(config =>
		{
			config.AddEnvironmentVariables(prefix: "DOTNET_");
			if (args is { Length: > 0 })
			{
				config.AddCommandLine(args);
			}
		});


		builder.ConfigureAppConfiguration((hostingContext, config) =>
		{
			config.AddEnvironmentVariables();

			if (args is { Length: > 0 })
			{
				config.AddCommandLine(args);
			}
		});

		builder.ConfigureLogging((hostingContext, logging) =>
		{
			logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
			logging.Configure(options =>
			{
				options.ActivityTrackingOptions =
					ActivityTrackingOptions.SpanId |
					ActivityTrackingOptions.TraceId |
					ActivityTrackingOptions.ParentId;
			});

		});

		builder.UseDefaultServiceProvider((context, options) =>
		{
				//bool isDevelopment = context.HostingEnvironment.IsDevelopment();
				options.ValidateScopes = false; // isDevelopment;
				options.ValidateOnBuild = false;// isDevelopment;
			});

		return builder;
	}

}
