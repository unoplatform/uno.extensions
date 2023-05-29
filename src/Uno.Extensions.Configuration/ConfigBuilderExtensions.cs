namespace Uno.Extensions.Configuration;

public static class ConfigBuilderExtensions
{
	public const string ConfigurationFolderName = "config";
	public static IConfigBuilder ContentSource(this IConfigBuilder hostBuilder, string? config = null, bool includeEnvironmentSettings = true)
	{
		return hostBuilder
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

	public static IConfigBuilder EmbeddedSource<TApplicationRoot>(this IConfigBuilder hostBuilder, string? config = null, bool includeEnvironmentSettings = true)
		where TApplicationRoot : class
	{
		return hostBuilder
				.ConfigureAppConfiguration((ctx, b) =>
				{
					if (config is { Length: > 0 })
					{
						b.AddEmbeddedConfiguration<TApplicationRoot>(ctx, config);
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

	public static IConfigBuilder Section<TSettingsOptions>(
		this IConfigBuilder hostBuilder,
		string configurationSection)
			where TSettingsOptions : class, new()
	{
		return hostBuilder.Section<TSettingsOptions>(ctx => ctx.Configuration.GetSection(configurationSection));
	}

	public static IConfigBuilder Section<TSettingsOptions>(
		this IConfigBuilder hostBuilder,
		Func<HostBuilderContext, IConfigurationSection>? configSection = null)
			where TSettingsOptions : class, new()
	{
		if (configSection is null)
		{
			configSection = ctx => ctx.Configuration.GetSection(typeof(TSettingsOptions).Name);
		}

		static string? FilePath(HostBuilderContext hctx)
		{
			var file = $"{ConfigurationFolderName}/{string.Format(AppConfiguration.FileNameTemplate, typeof(TSettingsOptions).Name)}";
			var appData = hctx.HostingEnvironment.GetAppDataPath();
			if(appData is null)
			{
				return default;
			}
			var path = Path.Combine(appData, file);
			return path;
		}

		return hostBuilder
			.ConfigureAppConfiguration((ctx, b) =>
			{
				var path = FilePath(ctx);
				b.AddJsonFile(path, optional: true, reloadOnChange: false); // In .NET6 we can enable this again because we can use polling
			})
				.ConfigureServices((ctx, services) =>
				{
					var configPath = FilePath(ctx);
					if (configPath is null)
					{
						return;
					}

					var section = configSection(ctx);
					services.ConfigureAsWritable<TSettingsOptions>(section, configPath);
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
