namespace Uno.Extensions.Configuration;

internal class Reloader
{
	internal static SemaphoreSlim ReadWriteLock = new SemaphoreSlim(1);

	private ILogger Logger { get; }

	private IConfigurationRoot Config { get; }

	public Reloader(ILogger<ReloadService> logger, IConfigurationRoot configRoot)
	{
		Logger = logger;
		Config = configRoot;
	}

	public async Task ReloadAllFileConfigurationProviders(string? configFile = default)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Reloading configuration - started");
		}

		var fileProviders = Config.Providers;
		foreach (var fp in fileProviders)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($@"Config provider of type '{fp.GetType().Name}'");
			}

			if (fp is FileConfigurationProvider fcp &&
				fcp.Source.Path is { Length: > 0 } &&
				(configFile is null || configFile.ToLower().Contains(fcp.Source.Path.Split('/', '\\').Last().ToLower())))
			{
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.LogTraceMessage($@"Loading from file '{fcp.Source.Path}'");
				}

				var provider = fcp.Source.FileProvider;
				var info = provider?.GetFileInfo(fcp.Source.Path ?? string.Empty);
				if (info is not null)
				{
					if (!File.Exists(info.PhysicalPath))
					{
						if (Logger.IsEnabled(LogLevel.Trace))
						{
							Logger.LogTraceMessage($@"File doesn't exist '{info.PhysicalPath}'");
						}
					}
					else
					{
						if (Logger.IsEnabled(LogLevel.Trace))
						{
							var contents = File.ReadAllText(info.PhysicalPath);
							Logger.LogTraceMessage($@"Contents '{contents}'");
							Logger.LogTraceMessage($@"Loading from full path '{info.PhysicalPath}'");
						}

						await ReadWriteLock.WaitAsync();
						try
						{
							fp.Load();
						}
						finally
						{
							ReadWriteLock.Release();
						}
					}
				}
			}
		}
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Reloading configuration complete");
		}
	}
}
