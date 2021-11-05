using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Windows.Storage;

namespace Uno.Extensions.Configuration
{
    public class ReloadService : IHostedService
    {
        public ReloadService(
            ILogger<ReloadService> logger,
            Reloader reload,
            IHostEnvironment hostEnvironment)
        {
            Logger.LogDebugMessage($"Created");
            Logger = logger;
            Reload = reload;
            HostEnvironment = hostEnvironment;
        }

        private ILogger Logger { get; }

        private Reloader Reload { get; }

        private IHostEnvironment HostEnvironment { get; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var folder = await localFolder.CreateFolderAsync(HostBuilderExtensions.ConfigurationFolderName, CreationCollisionOption.OpenIfExists);
            Logger.LogDebugMessage($@"Folder path should be '{folder.Path}'");

            await CopyApplicationFileToLocal(localFolder, "appsettings.json");
            await CopyApplicationFileToLocal(localFolder, $"appsettings.{HostEnvironment.EnvironmentName}.json");

            Logger.LogDebugMessage($"Started");
            await Reload.ReloadAllFileConfigurationProviders();
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
            catch (Exception ex)
            {
                Logger.LogTrace($"{file} not included as content");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebugMessage($"Stopped");
            return Task.CompletedTask;
        }
    }
}
