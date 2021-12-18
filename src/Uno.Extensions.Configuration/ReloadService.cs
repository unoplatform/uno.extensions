using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Hosting;
using Uno.Extensions.Logging;
using Uno.Extensions.Storage;

namespace Uno.Extensions.Configuration;

public class ReloadService : IHostedService, IStartupService
{
	public ReloadService(
		ILogger<ReloadService> logger,
		Reloader reload,
		IHostEnvironment hostEnvironment,
		IConfigurationRoot configRoot,
		IStorageProxy storage)
	{
		Logger = logger;
		Reload = reload;
		Config = configRoot;
		HostEnvironment = hostEnvironment;
		Storage = storage;
		if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Created");
	}

	private IStorageProxy Storage { get; }

	private TaskCompletionSource<bool> StartupCompletion { get; } = new TaskCompletionSource<bool>();

	private IConfigurationRoot Config { get; }

	private ILogger Logger { get; }

	private Reloader Reload { get; }

	private IHostEnvironment HostEnvironment { get; }

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		//var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
		//var folder = await localFolder.CreateFolderAsync(HostBuilderExtensions.ConfigurationFolderName, CreationCollisionOption.OpenIfExists);
		var folderPath = await Storage.CreateLocalFolder(HostBuilderExtensions.ConfigurationFolderName);
		if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($@"Folder path should be '{folderPath}'");

		var fileProviders = Config.Providers;
		var reloadEnabled = false;
		var appSettings = false;
		var envAppSettings = false;
		var appSettingsFileName = $"{AppSettings.AppSettingsFileName}.json";
		var environmentAppSettingsFileName = $"{AppSettings.AppSettingsFileName}.{HostEnvironment.EnvironmentName}.json".ToLower();
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
			await CopyApplicationFileToLocal(folderPath, $"{AppSettings.AppSettingsFileName}.json".ToLower());
		}

		if (envAppSettings)
		{
			await CopyApplicationFileToLocal(folderPath, $"{AppSettings.AppSettingsFileName}.{HostEnvironment.EnvironmentName}.json".ToLower());
		}

		if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Started");

		if (reloadEnabled)
		{
			await Reload.ReloadAllFileConfigurationProviders();
		}

		StartupCompletion.TrySetResult(true);
	}

	private async Task CopyApplicationFileToLocal(string localFolderPath, string file)
	{
		try
		{
			var settings = await Storage.ReadFromApplicationFile(file);
			if (!string.IsNullOrWhiteSpace(settings))
			{
				if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($@"Settings '{settings}'");
				var fullPath = Path.Combine(localFolderPath, file);
				await Storage.WriteToFile(fullPath, settings);
			}
		}
		catch
		{
			Logger.LogTrace($"{file} not included as content");
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Stopped");
		return Task.CompletedTask;
	}

	public Task StartupComplete()
	{
		return StartupCompletion.Task;
	}
}

