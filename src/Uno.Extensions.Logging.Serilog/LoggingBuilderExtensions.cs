using System;
using System.IO;

using Microsoft.Extensions.Logging;

using Serilog;

namespace Uno.Extensions;

/// <summary>
/// Extension methods to adjust the scope of collected logs. (log level)
/// </summary>
public static class LoggingBuilderExtensions
{
	public static ILoggingBuilder AddSerilog(
        this ILoggingBuilder builder,
        bool consoleLoggingEnabled = false,
        bool fileLoggingEnabled = false,
        Action<LoggerConfiguration>? configureLogger = null)
	{
#pragma warning disable CS0436
        var loggerConfiguration = new LoggerConfiguration();
        // loggerConfiguration.ReadFrom.Configuration(context.Configuration);
        if (consoleLoggingEnabled)
        {
            HostBuilderExtensions.AddConsoleLogging(loggerConfiguration);
        }
        if (fileLoggingEnabled)
        {
            var logPath = GetLogFilePath();
            if (logPath is not null)
            {
                HostBuilderExtensions.AddFileLogging(loggerConfiguration, logPath);
            }
        }
#pragma warning restore CS0436
        configureLogger?.Invoke(loggerConfiguration);
        var logger = loggerConfiguration.CreateLogger();

        builder.AddSerilog(logger);
		return builder;
	}

    private static string? GetLogFilePath()
    {
        var logDirectory = ApplicationDataExtensions.DataFolder();

        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            return null;
        }

        var assemblyName = PlatformHelper.GetAppAssembly()?.FullName ?? "unologging";
        return Path.Combine(logDirectory, $"{assemblyName}.log");
    }
}
