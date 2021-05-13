using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Chinook.DynamicMvvm;
using Microsoft.Extensions.Logging;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace ApplicationTemplate.Presentation
{
    public partial class SummaryDiagnosticsViewModel : ViewModel
    {
        private readonly DateTimeOffset _now = DateTimeOffset.Now;
        private readonly DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

        public string Summary => this.Get(GetSummary);

        public IDynamicCommand SendSummary => this.GetCommandFromTask(async ct =>
        {
            var summary = GetSummary();

            var message = new EmailMessage
            {
                Subject = $"Diagnostics - {GetType().Assembly.GetName().Name} ({_now})",
                Body = summary,
            };

            foreach (var logFilePath in LoggingConfiguration.FileLogging.GetLogFilePaths())
            {
                if (File.Exists(logFilePath))
                {
                    message.Attachments.Add(new EmailAttachment(logFilePath));
                }
            }

            await RunOnDispatcher(ct, _ => this.GetService<IEmail>().ComposeAsync(message));

            this.GetService<ILogger<SummaryDiagnosticsViewModel>>().LogInformation("Environment summary sent.");
        });

        private string GetSummary()
        {
            var appInfo = this.GetService<IAppInfo>();
            var deviceInfo = this.GetService<IDeviceInfo>();

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Project: {GetType().Assembly.GetName().Name}");

            stringBuilder.AppendLine($"Date on device: {_now}");

            stringBuilder.AppendLine($"Date on device (UTC): {_utcNow}");

            stringBuilder.AppendLine($"Version string: {appInfo.VersionString}");
            stringBuilder.AppendLine($"Version: {appInfo.Version}");

            stringBuilder.AppendLine($"Build number: {appInfo.Version.Build}");
            stringBuilder.AppendLine($"Build string: {appInfo.BuildString}");

            stringBuilder.AppendLine($"OS Version: {deviceInfo.Version}");

            stringBuilder.AppendLine($"Device type: {deviceInfo.DeviceType}");
            stringBuilder.AppendLine($"Device type: {deviceInfo.Idiom}");

            stringBuilder.AppendLine($"Device name: {deviceInfo.Manufacturer} {deviceInfo.Model}");

            // UserAgent Not available in X.E but we could do it in app, here's the implementation
            // stringBuilder.AppendLine($"User agent: {environmentService.UserAgent}");
            // Android: UserAgent = $"{applicationName}/{AppVersion.ToString()}({DeviceType}; Android {OSVersionNumber})";
            // iOS :UserAgent = $"{applicationName}/{AppVersion.ToString()}({DeviceType}; iOS {OSVersionNumber})";
            stringBuilder.AppendLine($"Culture: {CultureInfo.CurrentCulture.Name}");

            stringBuilder.AppendLine($"Environment: {AppSettingsConfiguration.AppEnvironment.GetCurrent()}");

            stringBuilder.AppendLine($"Build environment: {AppSettingsConfiguration.DefaultEnvironment}");

//-:cnd:noEmit
#if DEBUG
//+:cnd:noEmit
            var isDebug = true;
//-:cnd:noEmit
#else
//+:cnd:noEmit
            var isDebug = false;
//-:cnd:noEmit
#endif
//+:cnd:noEmit
            stringBuilder.AppendLine($"Debug build: {isDebug}");

            stringBuilder.AppendLine($"Console logging enabled: {LoggingConfiguration.ConsoleLogging.GetIsEnabled()}");

            stringBuilder.AppendLine($"File logging enabled: {LoggingConfiguration.FileLogging.GetIsEnabled()}");

            var hasLogFile = File.Exists(LoggingConfiguration.FileLogging.GetLogFilePath());
            stringBuilder.AppendLine($"Has log file: {hasLogFile}");

            stringBuilder.Append(GetStartupDetails());

            return stringBuilder.ToString();
        }

        private string GetStartupDetails()
        {
            var stringBuilder = new StringBuilder();

//-:cnd:noEmit
#if !NETFRAMEWORK
//+:cnd:noEmit
            var startup = App.Startup;
            var startupTime = startup.StartActivity.StartTimeUtc + startup.StartActivity.Duration - startup.PreInitializeActivity.StartTimeUtc;

            stringBuilder.AppendLine($"Startup time: {Math.Round(startupTime.TotalMilliseconds)} ms");

            stringBuilder.AppendLine(GetFormattedActivity(startup.PreInitializeActivity));
            stringBuilder.AppendLine(GetFormattedActivity(startup.InitializeActivity, startup.PreInitializeActivity));
            stringBuilder.AppendLine(GetFormattedActivity(startup.CoreStartup.BuildCoreHostActivity, prefix: "  "));
            stringBuilder.AppendLine(GetFormattedActivity(startup.CoreStartup.BuildHostActivity, prefix: "  "));
            stringBuilder.AppendLine(GetFormattedActivity(App.Instance.ShellActivity, startup.InitializeActivity));
            stringBuilder.AppendLine(GetFormattedActivity(startup.StartActivity, App.Instance.ShellActivity));

            string GetFormattedActivity(Activity activity, Activity previousActivity = null, string prefix = null)
            {
                var sb = new StringBuilder();

                if (prefix != null)
                {
                    sb.Append(prefix);
                }

                sb.Append($"- {activity.OperationName}");
                sb.Append($": {Math.Round(activity.Duration.TotalMilliseconds)} ms");
                sb.Append($" @ {activity.StartTimeUtc.ToString("hh:mm:ss.fff", CultureInfo.InvariantCulture)}");

                // This is the time between the start of the current activity and the end of the previous activity.
                // We want this to be as low as possible otherwise the actvity itself might not be taking long
                // but the process (the sum of all the activities) may still be lengthy.
                var blankSpot = previousActivity != null
                    ? Math.Round((activity.StartTimeUtc - previousActivity.StartTimeUtc - previousActivity.Duration).TotalMilliseconds)
                    : default(double?);

                if (blankSpot != null)
                {
                    sb.Append($" [...{blankSpot} ms]");
                }

                return sb.ToString();
            }
//-:cnd:noEmit
#endif
//+:cnd:noEmit

            return stringBuilder.ToString();
        }
    }
}
