using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Windows.Storage;

namespace Uno.Extensions.Configuration
{
    public class Reloader
    {
        private ILogger Logger { get; }

        private IConfigurationRoot Config { get; }

        public Reloader(ILogger<ReloadService> logger, IConfigurationRoot configRoot)
        {
            Logger = logger;
            Config = configRoot;
        }

        public async Task ReloadAllFileConfigurationProviders(string configFile = null)
        {
            Logger.LogDebugMessage($"Reloading config");

            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var folder = await localFolder.CreateFolderAsync(HostBuilderExtensions.ConfigurationFolderName, CreationCollisionOption.OpenIfExists);
            Logger.LogDebugMessage($@"Folder path should be '{folder.Path}'");

            var fileProviders = Config.Providers;
            foreach (var fp in fileProviders)
            {
                Logger.LogDebugMessage($@"Config provider of type '{fp.GetType().Name}'");
                if (fp is FileConfigurationProvider fcp && (configFile is null || configFile.Contains(fcp.Source.Path, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Logger.LogDebugMessage($@"Loading from file '{fcp.Source.Path}'");
                    var provider = fcp.Source.FileProvider;
                    var info = provider.GetFileInfo(fcp.Source.Path);
                    Logger.LogDebugMessage($@"Loading from full path '{info.PhysicalPath}'");
                    fp.Load();
                }
            }
            Logger.LogDebugMessage($"Reloading configuration complete");
        }
    }
}
