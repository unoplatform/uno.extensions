using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chinook.DynamicMvvm;
using MessageDialogService;
using Microsoft.Extensions.Logging;

namespace ApplicationTemplate.Presentation
{
    public class LoggersDiagnosticsViewModel : ViewModel
    {
        public LoggersDiagnosticsViewModel()
        {
            AddDisposable(this.GetProperty(x => x.IsFileLoggingEnabled)
                .Observe()
                .SelectManyDisposePrevious((e, ct) => OnFileLoggingChanged(ct, e))
                .Subscribe()
            );

            AddDisposable(this.GetProperty(x => x.IsConsoleLoggingEnabled)
                .Observe()
                .SelectManyDisposePrevious((e, ct) => OnConsoleLoggingChanged(ct, e))
                .Subscribe()
            );
        }

        public bool IsFileLoggingEnabled
        {
            get => this.Get(initialValue: LoggingConfiguration.FileLogging.GetIsEnabled());
            set => this.Set(value);
        }

        public bool IsConsoleLoggingEnabled
        {
            get => this.Get(initialValue: LoggingConfiguration.ConsoleLogging.GetIsEnabled());
            set => this.Set(value);
        }

        public IDynamicCommand DeleteLogFiles => this.GetCommandFromTask(async ct =>
        {
            var confirmation = await this.GetService<IMessageDialogService>().ShowMessage(ct, mb => mb
                .Title("Diagnostics")
                .Content("Are you sure you want to delete the log files?")
                .CancelCommand()
                .Command(MessageDialogResult.Accept, label: "Delete")
            );

            if (confirmation != MessageDialogResult.Accept)
            {
                return;
            }

            LoggingConfiguration.FileLogging.DeleteLogFiles();

            this.GetService<ILogger<LoggersDiagnosticsViewModel>>().LogInformation("Log files deleted.");

            await this.GetService<IMessageDialogService>().ShowMessage(ct, mb => mb
                .Title("Diagnostics")
                .Content("Log files deleted.")
                .OkCommand()
            );
        });

        public IDynamicCommand TestLogs => this.GetCommandFromTask(async ct =>
        {
            var logger = this.GetService<ILogger<LoggersDiagnosticsViewModel>>();

            logger.LogCritical("Forced critical log. Please ignore.");
            logger.LogError("Forced error log. Please ignore.");
            logger.LogWarning("Forced warning log. Please ignore.");
            logger.LogInformation("Forced information log. Please ignore.");
            logger.LogDebug("Forced debug log. Please ignore.");
            logger.LogTrace("Forced trace log. Please ignore.");

            await this.GetService<IMessageDialogService>().ShowMessage(ct, mb => mb
                .Title("Diagnostics")
                .Content("Logs tested. Please check your logger outputs (console, file).")
                .OkCommand()
            );
        });

        private async Task OnConsoleLoggingChanged(CancellationToken ct, bool isEnabled)
        {
            var isCurrentlyEnabled = LoggingConfiguration.ConsoleLogging.GetIsEnabled();

            this.GetService<ILogger<LoggersDiagnosticsViewModel>>().LogInformation("{isEnabled} console logging.", isEnabled ? "Enabling" : "Disabling");

            LoggingConfiguration.ConsoleLogging.SetIsEnabled(isEnabled);

            if (isCurrentlyEnabled != isEnabled)
            {
                await this.GetService<IMessageDialogService>().ShowMessage(ct, mb => mb
                    .Title("Diagnostics")
                    .Content("Restart the application to apply your changes.")
                    .OkCommand()
                );
            }
        }

        private async Task OnFileLoggingChanged(CancellationToken ct, bool isEnabled)
        {
            var isCurrentlyEnabled = LoggingConfiguration.FileLogging.GetIsEnabled();

            this.GetService<ILogger<LoggersDiagnosticsViewModel>>().LogInformation("{isEnabled} file logging.", isEnabled ? "Enabling" : "Disabling");

            LoggingConfiguration.FileLogging.SetIsEnabled(isEnabled);

            if (isCurrentlyEnabled != isEnabled)
            {
                await this.GetService<IMessageDialogService>().ShowMessage(ct, mb => mb
                   .Title("Diagnostics")
                   .Content("Restart the application to apply your changes.")
                   .OkCommand()
               );
            }
        }
    }
}
