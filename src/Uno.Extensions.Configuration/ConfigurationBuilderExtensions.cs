namespace Uno.Extensions.Configuration;

public static class ConfigurationBuilderExtensions
{
	public static IConfigurationBuilder AddConfiguration(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext, string configurationFileName)
	{
		var relativePath = $"{HostBuilderExtensions.ConfigurationFolderName}/{configurationFileName}";
		var rootFolder = (hostingContext.HostingEnvironment as IAppHostEnvironment)?.AppDataPath ?? String.Empty;
		var fullPath = Path.Combine(rootFolder, relativePath);
		return configurationBuilder.AddJsonFile(fullPath, optional: true, reloadOnChange: false);
	}

	public static IConfigurationBuilder AddAppConfiguration(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
	{
		return configurationBuilder.AddConfiguration(hostingContext, AppConfiguration.FileName);
	}

	public static IConfigurationBuilder AddEnvironmentAppConfiguration(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
	{
		var env = hostingContext.HostingEnvironment;
		return configurationBuilder.AddConfiguration(hostingContext, string.Format(AppConfiguration.FileNameTemplate, env.EnvironmentName).ToLower());
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

	public static IConfigurationBuilder AddEmbeddedAppConfiguration<TApplicationRoot>(this IConfigurationBuilder configurationBuilder)
		where TApplicationRoot : class
	{
		var generalAppConfigurationFileName = AppConfiguration.FileName;
		return configurationBuilder.AddEmbeddedConfiguration<TApplicationRoot>(generalAppConfigurationFileName);
	}

	public static IConfigurationBuilder AddEmbeddedEnvironmentAppConfiguration<TApplicationRoot>(
		this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
			where TApplicationRoot : class
	{
		var env = hostingContext.HostingEnvironment;

		var environmentAppConfigurationFileName = string.Format(AppConfiguration.FileNameTemplate, env.EnvironmentName).ToLower();
		return configurationBuilder.AddEmbeddedConfiguration<TApplicationRoot>(environmentAppConfigurationFileName);
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
