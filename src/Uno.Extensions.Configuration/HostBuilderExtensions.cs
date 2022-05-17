namespace Uno.Extensions.Configuration;

public static class HostBuilderExtensions
{
	public const string ConfigurationFolderName = "config";

	public static IHostBuilder UseConfiguration(
		this IHostBuilder hostBuilder,
		Action<IServiceCollection> configure)
	{
		return hostBuilder.UseConfiguration((context, builder) => configure.Invoke(builder));
	}

	public static IHostBuilder UseConfiguration(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection>? configure = default)
	{
		return hostBuilder
				.ConfigureServices((ctx, s) =>
				{
					s.TryAddSingleton(a => ctx.Configuration);
					s.TryAddSingleton(a => (IConfigurationRoot)ctx.Configuration);
					s.TryAddSingleton<Reloader>();
					s.TryAddSingleton<ReloadService>();
					_ = s.AddHostedService(sp => sp.GetRequiredService<ReloadService>());
					s.TryAddSingleton<IStartupService>(sp => sp.GetRequiredService<ReloadService>());
					configure?.Invoke(ctx, s);
				});
	}

	public static IHostBuilder UseAppConfiguration(this IHostBuilder hostBuilder, bool includeEnvironmentSettings = true)
	{
		return hostBuilder
				.UseConfiguration()
				.ConfigureAppConfiguration((ctx, b) =>
				{
					b.AddAppConfiguration(ctx);
					if (includeEnvironmentSettings)
					{
						b.AddEnvironmentAppConfiguration(ctx);
					}
				});
	}

	public static IHostBuilder UseCustomConfiguration(this IHostBuilder hostBuilder, string customSettingsFileName)
	{
		return hostBuilder
				.UseConfiguration()
				.ConfigureAppConfiguration((ctx, b) =>
				{
					b.AddConfiguration(ctx, customSettingsFileName);
				});
	}

	public static IHostBuilder UseEmbeddedAppConfiguration<TApplicationRoot>(this IHostBuilder hostBuilder, bool includeEnvironmentSettings = true)
		where TApplicationRoot : class
	{
		return hostBuilder
				.UseConfiguration()
				.ConfigureAppConfiguration((ctx, b) =>
				{
					b.AddEmbeddedAppConfiguration<TApplicationRoot>();
					if (includeEnvironmentSettings)
					{
						b.AddEmbeddedEnvironmentAppConfiguration<TApplicationRoot>(ctx);
					}
				});
	}

	public static IHostBuilder UseCustomEmbeddedConfiguration<TApplicationRoot>(this IHostBuilder hostBuilder, string customSettingsFileName)
		where TApplicationRoot : class
	{
		return hostBuilder
				.UseConfiguration()
				.ConfigureAppConfiguration((ctx, b) =>
				{
					b.AddEmbeddedConfiguration<TApplicationRoot>(customSettingsFileName);
				});
	}

	public static IHostBuilder UseSettings<TSettingsOptions>(
			this IHostBuilder hostBuilder,
			Func<HostBuilderContext, IConfigurationSection>? configSection = null)
				where TSettingsOptions : class, new()
	{
		if (configSection is null)
		{
			configSection = ctx => ctx.Configuration.GetSection(typeof(TSettingsOptions).Name);
		}

		static string FilePath(HostBuilderContext hctx)
		{
			var file = $"{ConfigurationFolderName}/{string.Format(AppConfiguration.FileNameTemplate,typeof(TSettingsOptions).Name)}";
			var appData = (hctx.HostingEnvironment as IAppHostEnvironment)?.AppDataPath ?? string.Empty;
			var path = Path.Combine(appData, file);
			return path;
		}

		return hostBuilder
			.UseConfiguration()
			.ConfigureAppConfiguration((ctx, b) =>
				{
					var path = FilePath(ctx);
					b.AddJsonFile(path, optional: true, reloadOnChange: false); // In .NET6 we can enable this again because we can use polling
				})
				.ConfigureServices((ctx, services) =>
				{
					var section = configSection(ctx);
					services.ConfigureAsWritable<TSettingsOptions>(section, FilePath(ctx));
				}

			);
	}


	public static IHostBuilder UseConfiguration<TOptions>(this IHostBuilder hostBuilder, string? configurationSection = null)
		where TOptions : class
	{
		if (configurationSection is null)
		{
			configurationSection = typeof(TOptions).Name;
		}

		return hostBuilder
			.UseConfiguration()
			.ConfigureServices((ctx, services) => services.Configure<TOptions>(ctx.Configuration.GetSection(configurationSection)));
	}

	public static IHostBuilder AddConfigurationSectionFromEntity<TEntity>(
		this IHostBuilder hostBuilder,
		TEntity entity,
		string? sectionName = default)
	{
		return hostBuilder
				.ConfigureHostConfiguration(
					configurationBuilder => configurationBuilder.AddSectionFromEntity(entity, sectionName));
	}
}
