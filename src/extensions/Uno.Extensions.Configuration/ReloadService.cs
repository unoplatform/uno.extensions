using System;
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
        public ReloadService(ILogger<ReloadService> logger, Reloader reload)
        {
            Logger = logger;
            Reload = reload;
        }

        private ILogger Logger { get; }

        private Reloader Reload { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebugMessage($"Started");
            return Reload.ReloadAllFileConfigurationProviders();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebugMessage($"Stopped");
            return Task.CompletedTask;
        }
    }
}
