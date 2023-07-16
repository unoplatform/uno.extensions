namespace Uno.Extensions.Configuration;

/// <summary>
/// Extension methods for registering sources with the <see cref="IConfigurationBuilder"/> 
/// </summary>
public static class ConfigurationBuilderExtensions
{
	private static IConfigurationBuilder AddConfigurationFile(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext, string configurationFileName)
	{
		var relativePath = $"{ConfigBuilderExtensions.ConfigurationFolderName}/{configurationFileName}";
		var rootFolder = hostingContext.HostingEnvironment.GetAppDataPath();
		if (rootFolder is null)
		{
			return configurationBuilder;
		}
		var fullPath = Path.Combine(rootFolder, relativePath);
		return configurationBuilder.AddJsonFile(fullPath, optional: true, reloadOnChange: false);
	}

	/// <summary>
	/// Adds a JSON configuration source by name to builder.
	/// </summary>
	/// <param name="configurationBuilder">The builder instance used to create an <see cref="IConfiguration"/> with keys and values from a set of sources</param>
	/// <param name="hostingContext">The <see cref="HostBuilderContext"/> which provides information specific to the hosting environment</param>
	/// <param name="configurationName">A name to identify the added configuration source. Optional</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddConfiguration(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext, string? configurationName = null)
	{
		var configSection = configurationName is { Length: > 0 } ? string.Format(AppConfiguration.FileNameTemplate, configurationName) : AppConfiguration.FileName;
		return configurationBuilder.AddConfigurationFile(hostingContext, configSection.ToLower());
	}

	/// <summary>
	/// Adds a JSON configuration source by name to builder. 
	/// The resultant configuration file name is suffixed with the current hosting environment name.
	/// </summary>
	/// <param name="configurationBuilder">The builder instance used to create an <see cref="IConfiguration"/> with keys and values from a set of sources</param>
	/// <param name="hostingContext">The <see cref="HostBuilderContext"/> which provides information specific to the hosting environment</param>
	/// <param name="configurationName">A name to identify the added configuration source. Optional</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddEnvironmentConfiguration(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext, string? configurationName = null)
	{
		var env = hostingContext.HostingEnvironment;
		var configSection = configurationName is { Length: > 0 } ? $"{configurationName}.{env.EnvironmentName}" : env.EnvironmentName;
		return configurationBuilder.AddConfigurationFile(hostingContext, string.Format(AppConfiguration.FileNameTemplate, configSection).ToLower());
	}

	/// <summary>
	/// Adds a JSON configuration source with the default name to builder.
	/// </summary>
	/// <param name="configurationBuilder">The builder instance used to create an <see cref="IConfiguration"/> with keys and values from a set of sources</param>
	/// <param name="hostingContext">The <see cref="HostBuilderContext"/> which provides information specific to the hosting environment</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddAppConfiguration(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
	{
		return configurationBuilder.AddConfigurationFile(hostingContext, AppConfiguration.FileName);
	}

	/// <summary>
	/// Adds a JSON configuration source with the default name suffixed containing the current hosting environment name to builder.
	/// </summary>
	/// <param name="configurationBuilder">The builder instance used to create an <see cref="IConfiguration"/> with keys and values from a set of sources</param>
	/// <param name="hostingContext">The <see cref="HostBuilderContext"/> which provides information specific to the hosting environment</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddEnvironmentAppConfiguration(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
	{
		return configurationBuilder.AddEnvironmentConfiguration(hostingContext, default);
	}

	public static IConfigurationBuilder AddEmbeddedConfigurationFile<TApplicationRoot>(this IConfigurationBuilder configurationBuilder, string configurationFileName)
		where TApplicationRoot : class
	{
		var generalAppConfiguration =
			EmbeddedAppConfigurationFile.AllFiles<TApplicationRoot>()
			.FirstOrDefault(s => s.FileName.EndsWith(configurationFileName, StringComparison.OrdinalIgnoreCase));

		if (generalAppConfiguration != null)
		{
			configurationBuilder.AddJsonStream(generalAppConfiguration.GetContent());
		}

		return configurationBuilder;
	}

	/// <summary>
	/// Adds an embedded JSON configuration source by name to builder.
	/// </summary>
	/// <typeparam name="TApplicationRoot">The type of an assembly which should contain the embedded configuration file</typeparam>
	/// <param name="configurationBuilder">The builder instance used to create an <see cref="IConfiguration"/> with keys and values from a set of sources</param>
	/// <param name="hostingContext">The <see cref="HostBuilderContext"/> which provides information specific to the hosting environment</param>
	/// <param name="configurationName">A name to identify the added configuration source. Optional</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddEmbeddedConfiguration<TApplicationRoot>(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext, string? configurationName = null)
		where TApplicationRoot : class
	{
		var configSection = configurationName is { Length: > 0 } ? string.Format(AppConfiguration.FileNameTemplate, configurationName) : AppConfiguration.FileName;
		return configurationBuilder.AddEmbeddedConfigurationFile<TApplicationRoot>(configSection.ToLower());
	}

	/// <summary>
	/// Adds an embedded JSON configuration source by name to builder.
	/// The resultant configuration file name is suffixed with the current hosting environment name.
	/// </summary>
	/// <typeparam name="TApplicationRoot">The type of an assembly which should contain the embedded configuration file</typeparam>
	/// <param name="configurationBuilder">The builder instance used to create an <see cref="IConfiguration"/> with keys and values from a set of sources</param>
	/// <param name="hostingContext">The <see cref="HostBuilderContext"/> which provides information specific to the hosting environment</param>
	/// <param name="configurationName">A name to identify the added configuration source. Optional</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddEnvironmentEmbeddedConfiguration<TApplicationRoot>(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext, string? configurationName = null)
		where TApplicationRoot : class
	{
		var env = hostingContext.HostingEnvironment;
		var configSection = configurationName is { Length: > 0 } ? $"{configurationName}.{env.EnvironmentName}" : env.EnvironmentName;
		return configurationBuilder.AddEmbeddedConfigurationFile<TApplicationRoot>(string.Format(AppConfiguration.FileNameTemplate, configSection).ToLower());
	}

	/// <summary>
	/// Adds an embedded JSON configuration source with the default name to builder.
	/// </summary>
	/// <typeparam name="TApplicationRoot">The type of an assembly which should contain the embedded configuration file</typeparam>
	/// <param name="configurationBuilder">The builder instance used to create an <see cref="IConfiguration"/> with keys and values from a set of sources</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddEmbeddedAppConfiguration<TApplicationRoot>(this IConfigurationBuilder configurationBuilder)
		where TApplicationRoot : class
	{
		return configurationBuilder.AddEmbeddedConfigurationFile<TApplicationRoot>(AppConfiguration.FileName);
	}

	/// <summary>
	/// Adds an embedded JSON configuration source with the default name suffixed containing the current hosting environment name to builder.
	/// </summary>
	/// <typeparam name="TApplicationRoot">The type of an assembly which should contain the embedded configuration file</typeparam>
	/// <param name="configurationBuilder">The builder instance used to create an <see cref="IConfiguration"/> with keys and values from a set of sources</param>
	/// <param name="hostingContext">The <see cref="HostBuilderContext"/> which provides information specific to the hosting environment</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddEnvironmentEmbeddedAppConfiguration<TApplicationRoot>(
		this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
			where TApplicationRoot : class
	{
		return configurationBuilder.AddEnvironmentEmbeddedConfiguration<TApplicationRoot>(hostingContext, default);
	}

	/// <summary>
	/// Adds a configuration section which contains keys and values from an entity to builder.
	/// </summary>
	/// <typeparam name="TEntity">The type of entity with configuration information to serialize</typeparam>
	/// <param name="configurationBuilder">The builder instance used to create an <see cref="IConfiguration"/> with keys and values from a set of sources</param>
	/// <param name="entity">An entity of the specified type parameter to be serialized</param>
	/// <param name="sectionName">A name for the added configuration section. Optional</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddSectionFromEntity<TEntity>(
		this IConfigurationBuilder configurationBuilder,
		TEntity entity,
		string? sectionName = null)
	{
		return configurationBuilder
			.AddJsonStream(
				new MemoryStream(
					Encoding.ASCII.GetBytes(
						JsonSerializer.Serialize(
							new Dictionary<string, TEntity>
							{
									{ sectionName ?? typeof(TEntity).Name, entity }
							}))));
	}
}
