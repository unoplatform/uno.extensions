using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chinook.DynamicMvvm;
using MessageDialogService;
using Microsoft.Extensions.Logging;
using Windows.Storage;
using Windows.System;

namespace ApplicationTemplate.Presentation
{
    public class SettingsDiagnosticsViewModel : ViewModel
    {
        public SettingsDiagnosticsViewModel()
        {
            AddDisposable(this.GetProperty(x => x.IsDiagnosticsOverlayEnabled)
                .Observe()
                .SelectManyDisposePrevious((e, ct) => OnDiagnosticsOverlayChanged(ct, e))
                .Subscribe()
            );
        }

        public bool IsDiagnosticsOverlayEnabled
        {
            get => this.Get(initialValue: DiagnosticsConfiguration.DiagnosticsOverlay.GetIsEnabled());
            set => this.Set(value);
        }

        public IDynamicCommand OpenSettingsFolder => this.GetCommand(() =>
        {
            var localFolder = ApplicationData.Current.LocalFolder;

            this.GetService<IDispatcherScheduler>().ScheduleTask(async (ct2, s) =>
            {
//-:cnd:noEmit
#if !WINUI
//+:cnd:noEmit
                await Launcher.LaunchFolderAsync(localFolder).AsTask(ct2);
//-:cnd:noEmit
#endif
//+:cnd:noEmit
            });
        });

        public bool CanOpenSettingsFolder { get; } =
//-:cnd:noEmit
#if !WINUI
//+:cnd:noEmit
            true;
//-:cnd:noEmit
#else
//+:cnd:noEmit
            false;
        //-:cnd:noEmit
#endif
        //+:cnd:noEmit

        private async Task OnDiagnosticsOverlayChanged(CancellationToken ct, bool isEnabled)
        {
            var isCurrentlyEnabled = DiagnosticsConfiguration.DiagnosticsOverlay.GetIsEnabled();

            this.GetService<ILogger<SettingsDiagnosticsViewModel>>().LogInformation("{isEnabled} diagnostics overlay.", isEnabled ? "Enabling" : "Disabling");

            DiagnosticsConfiguration.DiagnosticsOverlay.SetIsEnabled(isEnabled);

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
