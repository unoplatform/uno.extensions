namespace Uno.Extensions.Configuration;

public class Reloader
{
	private ILogger Logger { get; }

	private IConfigurationRoot Config { get; }

	public Reloader(ILogger<ReloadService> logger, IConfigurationRoot configRoot)
	{
		Logger = logger;
		Config = configRoot;
	}

	public async Task ReloadAllFileConfigurationProviders(string? configFile = default)
	{
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Reloading config");


		var fileProviders = Config.Providers;
		foreach (var fp in fileProviders)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($@"Config provider of type '{fp.GetType().Name}'");
			if (fp is FileConfigurationProvider fcp && (configFile is null || configFile.ToLower().Contains(fcp.Source.Path.Split('/', '\\').Last().ToLower())))
			{
				if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($@"Loading from file '{fcp.Source.Path}'");
				var provider = fcp.Source.FileProvider;
				var info = provider.GetFileInfo(fcp.Source.Path);
				if (!File.Exists(info.PhysicalPath))
				{
					if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($@"File doesn't exist '{info.PhysicalPath}'");
				}
				else
				{
					var contents = File.ReadAllText(info.PhysicalPath);
					if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($@"Contents '{contents}'");
					if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($@"Loading from full path '{info.PhysicalPath}'");
					fp.Load();
				}
			}
		}
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Reloading configuration complete");
	}
}
