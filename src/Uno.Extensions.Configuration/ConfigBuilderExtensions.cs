namespace Uno.Extensions.Configuration;

/// <summary>
/// Extension methods for registering a configuration source with an instance
/// of <see cref="IConfigBuilder"/>.
/// </summary>
public static class ConfigBuilderExtensions
{
	/// <summary>
	/// Defines a default name for the folder containing configuration files.
	/// </summary>
	public const string ConfigurationFolderName = "config";

	/// <summary>
	/// Sets up the host builder to register content files by name as a configuration source
	/// </summary>
	/// <param name="hostBuilder">
	/// The <see cref="IConfigBuilder"/> to configure.
	/// </param>
	/// <param name="config">
	/// The name of the configuration file to register. Optional
	/// </param>
	/// <param name="includeEnvironmentSettings">
	/// Whether or not environment specific settings should be included. Optional
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IConfigBuilder"/> for chaining.
	/// </returns>
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

	/// <summary>
	/// Sets up the host builder to register embedded resource files from 
	/// the specified assembly as a configuration source
	/// </summary>
	/// <typeparam name="TApplicationRoot">
	/// The type that will be used to locate an assembly that contains the embedded resource files.
	/// </typeparam>
	/// <param name="hostBuilder">
	/// The <see cref="IConfigBuilder"/> to configure.
	/// </param>
	/// <param name="config">
	/// A name to identify the added configuration source. Optional
	/// </param>
	/// <param name="includeEnvironmentSettings">
	/// Whether or not environment specific settings should be included. Optional
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IConfigBuilder"/> for chaining.
	/// </returns>
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

	/// <summary>
	/// Sets up the host builder to register a specific configuration section
	/// </summary>
	/// <typeparam name="TSettingsOptions">
	/// The type that the configuration section will be deserialized to.
	/// </typeparam>
	/// <param name="hostBuilder">
	/// The <see cref="IConfigBuilder"/> to configure.
	/// </param>
	/// <param name="configurationSection">
	/// The configuration section to retrieve.
	/// </param>
	/// <param name="configSection">
	/// A delegate that returns the configuration section to retrieve.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IConfigBuilder"/> for chaining.
	/// </returns>
	public static IConfigBuilder Section<TSettingsOptions>(
		this IConfigBuilder hostBuilder,
		string? configurationSection = "",
		Func<HostBuilderContext, IConfigurationSection>? configSection = null)
			where TSettingsOptions : class, new()
	{
		if (configSection is null)
		{
			if (configurationSection is not { Length: > 0 })
			{
				configurationSection = typeof(TSettingsOptions).Name;
			}
			configSection = ctx => ctx.Configuration.GetSection(configurationSection);
		}

		static string? FilePath(HostBuilderContext hctx)
		{
			var file = $"{ConfigurationFolderName}/{string.Format(AppConfiguration.FileNameTemplate, typeof(TSettingsOptions).Name)}";
			var appData = hctx.HostingEnvironment.GetAppDataPath();
			if (appData is not { Length: > 0 })
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
				if (path is { Length: > 0 })
				{
					b.AddJsonFile(path, optional: true, reloadOnChange: false); // In .NET6 we can enable this again because we can use polling
				}
			})
				.ConfigureServices((ctx, services) =>
				{
					var configPath = FilePath(ctx);
					if (configPath is not { Length: > 0 })
					{
						return;
					}

					var section = configSection(ctx);
					services.ConfigureAsWritable<TSettingsOptions>(section, configPath, configurationSection);
				}

			).AsConfigBuilder();
	}

	/// <summary>
	/// Sets up the host builder to register a configuration section from an entity
	/// </summary>
	/// <typeparam name="TEntity">
	/// Represents the type of the entity specified in the <paramref name="entity"/> parameter.
	/// </typeparam>
	/// <param name="hostBuilder">
	/// The <see cref="IConfigBuilder"/> to configure.
	/// </param>
	/// <param name="entity">
	/// The entity of type <typeparamref name="TEntity"/> to retrieve the configuration section from.
	/// </param>
	/// <param name="sectionName">
	/// The configuration section name to retrieve. Optional
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IConfigBuilder"/> for chaining.
	/// </returns>
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
