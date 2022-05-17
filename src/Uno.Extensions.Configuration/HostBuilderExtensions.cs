namespace Uno.Extensions.Configuration;

public static class HostBuilderExtensions
{
	public const string ConfigurationFolderName = "config";

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

		hostBuilder = hostBuilder.ConfigureServices((ctx, s) =>
				{
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

	public static IConfigBuilder WithAppConfigFile(this IConfigBuilder hostBuilder, string? config = null, bool includeEnvironmentSettings = true)
	{
		return hostBuilder
				.UseConfiguration()
				.ConfigureAppConfiguration((ctx, b) =>
				{
					if (config is { Length: > 0 })
					{
						b.AddConfiguration(ctx, config);
						if (includeEnvironmentSettings)
						{
							b.AddEnvironmentConfiguration(ctx, config);
						}
					}
					else
					{
						b.AddAppConfiguration(ctx);
						if (includeEnvironmentSettings)
						{
							b.AddEnvironmentAppConfiguration(ctx);
						}
					}
				}).AsConfigBuilder();
	}

	public static IConfigBuilder WithEmbeddedAppConfigFile<TApplicationRoot>(this IConfigBuilder hostBuilder, string? config = null, bool includeEnvironmentSettings = true)
		where TApplicationRoot : class
	{
		return hostBuilder
				.UseConfiguration()
				.ConfigureAppConfiguration((ctx, b) =>
				{
					if (config is { Length: > 0 })
					{
						b.AddEmbeddedConfiguration<TApplicationRoot>(config);
						if (includeEnvironmentSettings)
						{
							b.AddEnvironmentConfiguration(ctx, config);
						}
					}
					else
					{
						b.AddEmbeddedAppConfiguration<TApplicationRoot>();
						if (includeEnvironmentSettings)
						{
							b.AddEnvironmentEmbeddedAppConfiguration<TApplicationRoot>(ctx);
						}
					}
				}).AsConfigBuilder();
	}

	public static IConfigBuilder RegisterSettings<TSettingsOptions>(
		this IConfigBuilder hostBuilder,
		string configurationSection)
			where TSettingsOptions : class, new()
	{
		return hostBuilder.RegisterSettings<TSettingsOptions>(ctx => ctx.Configuration.GetSection(configurationSection));
	}

	public static IConfigBuilder RegisterSettings<TSettingsOptions>(
		this IConfigBuilder hostBuilder,
		Func<HostBuilderContext, IConfigurationSection>? configSection = null)
			where TSettingsOptions : class, new()
	{
		if (configSection is null)
		{
			configSection = ctx => ctx.Configuration.GetSection(typeof(TSettingsOptions).Name);
		}

		static string FilePath(HostBuilderContext hctx)
		{
			var file = $"{ConfigurationFolderName}/{string.Format(AppConfiguration.FileNameTemplate, typeof(TSettingsOptions).Name)}";
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

			).AsConfigBuilder();
	}

	public static IConfigBuilder WithConfigurationSectionFromEntity<TEntity>(
		this IConfigBuilder hostBuilder,
		TEntity entity,
		string? sectionName = default)
	{
		return hostBuilder
				.ConfigureHostConfiguration(
					configurationBuilder => configurationBuilder.AddSectionFromEntity(entity, sectionName)).AsConfigBuilder();
	}
}

public interface IConfigBuilder : IHostBuilder
{
}

public record ConfigBuilder(IHostBuilder HostBuilder) : IConfigBuilder
{
	public IDictionary<object, object> Properties => HostBuilder.Properties;

	public IHost Build() => HostBuilder.Build();
	public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate) => HostBuilder.ConfigureAppConfiguration(configureDelegate);
	public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate) => HostBuilder.ConfigureContainer(configureDelegate);
	public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate) => HostBuilder.ConfigureHostConfiguration(configureDelegate);
	public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate) => HostBuilder.ConfigureServices(configureDelegate);
	public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull => HostBuilder.UseServiceProviderFactory(factory);
	public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull => HostBuilder.UseServiceProviderFactory(factory);
}

public static class ConfigBuilderExtensions
{
	public static IConfigBuilder AsConfigBuilder(this IHostBuilder hostBuilder)
	{
		return new ConfigBuilder(hostBuilder);
	}
}
