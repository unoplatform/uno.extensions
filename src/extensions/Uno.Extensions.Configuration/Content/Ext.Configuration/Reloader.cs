using System;
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
            Logger.LazyLogDebug(() => $"Reloading config");

            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var folder = await localFolder.CreateFolderAsync(HostBuilderExtensions.ConfigurationFolderName, CreationCollisionOption.OpenIfExists);
            Logger.LazyLogDebug(() => $@"Folder path should be '{folder.Path}'");

            var fileProviders = Config.Providers;
            foreach (var fp in fileProviders)
            {
                Logger.LazyLogDebug(() => $@"Config provider of type '{fp.GetType().Name}'");
                if (fp is FileConfigurationProvider fcp && (configFile is null || configFile.Contains(fcp.Source.Path, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Logger.LazyLogDebug(() => $@"Loading from file '{fcp.Source.Path}'");
                    var provider = fcp.Source.FileProvider;
                    var info = provider.GetFileInfo(fcp.Source.Path);
                    Logger.LazyLogDebug(() => $@"Loading from full path '{info.PhysicalPath}'");
                    fp.Load();
                }
            }
            Logger.LazyLogDebug(() => $"Reloading configuration complete");
        }
    }
}
