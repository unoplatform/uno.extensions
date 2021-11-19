using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Hosting;
using Uno.Extensions.Logging;
using Windows.Storage;

namespace Uno.Extensions.Configuration;

public class ReloadService : IHostedService, IStartupService
{
	public ReloadService(
		ILogger<ReloadService> logger,
		Reloader reload,
		IHostEnvironment hostEnvironment,
		IConfigurationRoot configRoot)
	{
		Logger = logger;
		Reload = reload;
		Config = configRoot;
		HostEnvironment = hostEnvironment;
		Logger.LogDebugMessage($"Created");
	}

	private TaskCompletionSource<bool> StartupCompletion { get; } = new TaskCompletionSource<bool>();

	private IConfigurationRoot Config { get; }

	private ILogger Logger { get; }

	private Reloader Reload { get; }

	private IHostEnvironment HostEnvironment { get; }

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
		var folder = await localFolder.CreateFolderAsync(HostBuilderExtensions.ConfigurationFolderName, CreationCollisionOption.OpenIfExists);
		Logger.LogDebugMessage($@"Folder path should be '{folder.Path}'");

		var fileProviders = Config.Providers;
		var reloadEnabled = false;
		var appSettings = false;
		var envAppSettings = false;
		var appSettingsFileName = $"{AppSettings.AppSettingsFileName}.json";
		var environmentAppSettingsFileName = $"{AppSettings.AppSettingsFileName}.{HostEnvironment.EnvironmentName}.json";
		foreach (var fp in fileProviders)
		{
			reloadEnabled = true;
			if (fp is FileConfigurationProvider fcp)
			{
				if (fcp.Source.Path.Contains(appSettingsFileName))
				{
					appSettings = true;
				}


				if (fcp.Source.Path.Contains(environmentAppSettingsFileName))
				{
					envAppSettings = true;
				}
			}

		}
		if (appSettings)
		{
			await CopyApplicationFileToLocal(localFolder, "appsettings.json");
		}

		if (envAppSettings)
		{
			await CopyApplicationFileToLocal(localFolder, $"appsettings.{HostEnvironment.EnvironmentName}.json");
		}

		Logger.LogDebugMessage($"Started");

		if (reloadEnabled)
		{
			await Reload.ReloadAllFileConfigurationProviders();
		}

		StartupCompletion.TrySetResult(true);
	}

	private async Task CopyApplicationFileToLocal(IStorageFolder localFolder, string file)
	{
		try
		{
			var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{file}"));
			if (File.Exists(storageFile.Path))
			{
				Logger.LogDebugMessage($@"Copying settings from '{storageFile.Path}'");
				var settings = File.ReadAllText(storageFile.Path);
				Logger.LogDebugMessage($@"Settings '{settings}'");
				var settingsFile = await localFolder.CreateFileAsync($"{file}", CreationCollisionOption.OpenIfExists);
				Logger.LogDebugMessage($@"Copying settings to '{settingsFile.Path}'");
				File.WriteAllText(settingsFile.Path, settings);
			}
		}
		catch
		{
			Logger.LogTrace($"{file} not included as content");
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		Logger.LogDebugMessage($"Stopped");
		return Task.CompletedTask;
	}

	public Task StartupComplete()
	{
		return StartupCompletion.Task;
	}
}

