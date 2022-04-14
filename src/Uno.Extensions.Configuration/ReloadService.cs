using System;
using System.Collections.Generic;
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
		IConfigurationRoot configRoot,
		IStorage storage)
	{
		Logger = logger;
		Reload = reload;
		Config = configRoot;
		Storage = storage;
		if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Created");
	}

	private IStorage Storage { get; }

	private TaskCompletionSource<bool> StartupCompletion { get; } = new TaskCompletionSource<bool>();

	private IConfigurationRoot Config { get; }

	private ILogger Logger { get; }

	private Reloader Reload { get; }

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		var folderPath = await Storage.CreateLocalFolderAsync(HostBuilderExtensions.ConfigurationFolderName);
		if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($@"Folder path should be '{folderPath}'");

		var fileProviders = Config.Providers;
		var reloadEnabled = false;
		var configSourceFiles = new List<string>();
		foreach (var fp in fileProviders)
		{
			if (fp is FileConfigurationProvider fcp)
			{
				reloadEnabled = true;
				// Sometimes fcp.Source.Path returns just filename, sometime config/filename
				configSourceFiles.Add(Path.GetFileName(fcp.Source.Path));
			}

		}

		foreach (var configSource in configSourceFiles)
		{
			await CopyApplicationFileToLocal(folderPath, configSource.ToLower());
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
			var settings = await Storage.ReadFileAsync(file);
			if (settings is not null &&
				!string.IsNullOrWhiteSpace(settings))
			{
				if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($@"Settings '{settings}'");
				var fullPath = Path.Combine(localFolderPath, file);
				await Storage.WriteFileAsync(fullPath, settings);
			}
		}
		catch
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTrace($"{file} not included as content");
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

