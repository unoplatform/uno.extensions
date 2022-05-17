namespace Uno.Extensions.Configuration;

public static class ConfigurationBuilderExtensions
{
	private static IConfigurationBuilder AddConfigurationFile(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext, string configurationFileName)
	{
		var relativePath = $"{HostBuilderExtensions.ConfigurationFolderName}/{configurationFileName}";
		var rootFolder = (hostingContext.HostingEnvironment as IAppHostEnvironment)?.AppDataPath ?? String.Empty;
		var fullPath = Path.Combine(rootFolder, relativePath);
		return configurationBuilder.AddJsonFile(fullPath, optional: true, reloadOnChange: false);
	}

	public static IConfigurationBuilder AddConfiguration(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext, string? configurationName = null)
	{
		var configSection = configurationName is { Length: > 0 } ? string.Format(AppConfiguration.FileNameTemplate, configurationName) : AppConfiguration.FileName;
		return configurationBuilder.AddConfigurationFile(hostingContext, configSection.ToLower());
	}

	public static IConfigurationBuilder AddEnvironmentConfiguration(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext, string? configurationName = null)
	{
		var env = hostingContext.HostingEnvironment;
		var configSection = configurationName is { Length: > 0 } ? $"{configurationName}.{env.EnvironmentName}" : env.EnvironmentName;
		return configurationBuilder.AddConfigurationFile(hostingContext, string.Format(AppConfiguration.FileNameTemplate, configSection).ToLower());
	}

	public static IConfigurationBuilder AddAppConfiguration(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
	{
		return configurationBuilder.AddConfigurationFile(hostingContext, AppConfiguration.FileName);
	}

	public static IConfigurationBuilder AddEnvironmentAppConfiguration(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
	{
		return configurationBuilder.AddEnvironmentConfiguration(hostingContext, default);
	}

	public static IConfigurationBuilder AddEmbeddedConfiguration<TApplicationRoot>(this IConfigurationBuilder configurationBuilder, string configurationFileName)
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

	public static IConfigurationBuilder AddEnvironmentEmbeddedConfiguration<TApplicationRoot>(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext, string? configurationName = null)
		where TApplicationRoot : class
	{
		var env = hostingContext.HostingEnvironment;
		var configSection = configurationName is { Length: > 0 } ? $"{configurationName}.{env.EnvironmentName}" : env.EnvironmentName;
		return configurationBuilder.AddEmbeddedConfiguration<TApplicationRoot>(string.Format(AppConfiguration.FileNameTemplate, configSection).ToLower());
	}

	public static IConfigurationBuilder AddEmbeddedAppConfiguration<TApplicationRoot>(this IConfigurationBuilder configurationBuilder)
		where TApplicationRoot : class
	{
		var generalAppConfigurationFileName = AppConfiguration.FileName;
		return configurationBuilder.AddEmbeddedConfiguration<TApplicationRoot>(generalAppConfigurationFileName);
	}

	public static IConfigurationBuilder AddEnvironmentEmbeddedAppConfiguration<TApplicationRoot>(
		this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
			where TApplicationRoot : class
	{
		return configurationBuilder.AddEnvironmentEmbeddedConfiguration<TApplicationRoot>(hostingContext, default);
	}

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
