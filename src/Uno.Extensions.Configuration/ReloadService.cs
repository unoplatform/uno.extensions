namespace Uno.Extensions.Configuration;

internal class ReloadService : IHostedService, IStartupService
{
	public ReloadService(
		ILogger<ReloadService> logger,
		Reloader reload,
		IConfigurationRoot configRoot,
		IStorage storage)
	{
		Logger = logger;
		Reload = reload;
		Config = configRoot;
		Storage = storage;
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Created");
		}
	}

	private IStorage Storage { get; }

	private TaskCompletionSource<bool> StartupCompletion { get; } = new TaskCompletionSource<bool>();

	private IConfigurationRoot Config { get; }

	private ILogger Logger { get; }

	private Reloader Reload { get; }

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		var folderPath = await Storage.CreateFolderAsync(ConfigBuilderExtensions.ConfigurationFolderName);
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($@"Application data path is '{folderPath}'");
		}

		var fileProviders = Config.Providers;
		var reloadEnabled = false;
		var configSourceFiles = new List<string>();
		foreach (var fp in fileProviders)
		{
			if (fp is FileConfigurationProvider fcp &&
				fcp.Source.Path is { Length: > 0 })
			{
				reloadEnabled = true;
				// Sometimes fcp.Source.Path returns just filename, sometime config/filename
				configSourceFiles.Add(Path.GetFileName(fcp.Source.Path));
			}

		}

		if (folderPath is not null)
		{
			foreach (var configSource in configSourceFiles)
			{
				await CopyApplicationFileToLocal(folderPath, configSource.ToLower());
			}
		}
		else
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($@"Application data path should not be null");
			}
		}


		if (reloadEnabled)
		{
			await Reload.ReloadAllFileConfigurationProviders();
		}

		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Startup completed");
		}

		StartupCompletion.TrySetResult(true);
	}

	private async Task CopyApplicationFileToLocal(string localFolderPath, string file)
	{
		try
		{
			var settings = await Storage.ReadPackageFileAsync(file);
			if (settings is not null &&
				!string.IsNullOrWhiteSpace(settings))
			{
				if (Logger.IsEnabled(LogLevel.Debug))
				{
					Logger.LogDebugMessage($@"Settings '{settings}'");
				}

				var fullPath = Path.Combine(localFolderPath, file);
				await Storage.WriteFileAsync(fullPath, settings, false);
			}
		}
		catch
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTrace($"{file} not included as content");
			}
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Stopped");
		}
		return Task.CompletedTask;
	}

	public Task StartupComplete()
	{
		return StartupCompletion.Task;
	}
}

