using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Uno.Extensions.Hosting;

namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IHostBuilder"/> to configure Serilog.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures logging to use Serilog with the default configuration.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IHostBuilder"/> to configure.
    /// </param>
    /// <param name="consoleLoggingEnabled">
    /// Whether to enable console logging. Optional.
    /// </param>
    /// <param name="fileLoggingEnabled">
    /// Whether to enable file logging. Optional.
    /// </param>
    /// <param name="configureLogger">
    /// Used to provide additional configuration for the logger. Optional.
    /// </param>
    /// <returns>
    /// The <see cref="IHostBuilder"/> that was passed in.
    /// </returns>
    public static IHostBuilder UseSerilog(this IHostBuilder hostBuilder,
        bool consoleLoggingEnabled = false,
        bool fileLoggingEnabled = false,
        Action<LoggerConfiguration>? configureLogger = null)
    {
        return hostBuilder.UseSerilog(() => consoleLoggingEnabled, () => fileLoggingEnabled, configureLogger);
    }

    /// <summary>
    /// Configures logging to use Serilog with the default configuration.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IHostBuilder"/> to configure.
    /// </param>
    /// <param name="consoleLoggingEnabled">
    /// Whether to enable console logging.
    /// </param>
    /// <param name="fileLoggingEnabled">
    /// Whether to enable file logging.
    /// </param>
    /// <param name="configureLogger">
    /// Used to provide additional configuration for the logger. Optional.
    /// </param>
    /// <returns>
    /// The <see cref="IHostBuilder"/> that was passed in.
    /// </returns>
    public static IHostBuilder UseSerilog(this IHostBuilder hostBuilder,
        Func<bool> consoleLoggingEnabled,
        Func<bool> fileLoggingEnabled,
        Action<LoggerConfiguration>? configureLogger = null)
    {
        return hostBuilder
                .ConfigureLogging((context, loggingBuilder) =>
                {
                    var loggerConfiguration = new LoggerConfiguration();

                    loggerConfiguration.ReadFrom.Configuration(context.Configuration);

                    if (consoleLoggingEnabled?.Invoke() ?? false)
                    {
                        AddConsoleLogging(loggerConfiguration);
                    }

                    if (fileLoggingEnabled?.Invoke() ?? false)
                    {
                        var logPath = GetLogFilePath(context);
                        if (logPath is not null)
                        {
                            AddFileLogging(loggerConfiguration, logPath);
                        }
                    }

                    configureLogger?.Invoke(loggerConfiguration);

                    var logger = loggerConfiguration.CreateLogger();

                    loggingBuilder.AddSerilog(logger);
                });
    }

    private static LoggerConfiguration AddConsoleLogging(LoggerConfiguration configuration)
    {
#pragma warning disable CA1416 // Validate platform compatibility: The net8.0 version is not used on older versions of OS
        return configuration
                //-:cnd:noEmit
#if __ANDROID__
                .WriteTo.AndroidLog(outputTemplate: "{Message:lj} {Exception}{NewLine}")
#elif __IOS__
                .WriteTo.NSLog(outputTemplate: "{Level:u1}/{SourceContext}: {Message:lj} {Exception}")
#else
                .WriteTo.Console(outputTemplate: "{Timestamp:MM-dd HH:mm:ss.fffzzz} {Level:u1}/{SourceContext}: {Message:lj} {Exception}{NewLine}")
#endif
                .WriteTo.Debug(outputTemplate: "{Timestamp:MM-dd HH:mm:ss.fffzzz} {Level:u1}/{SourceContext}: {Message:lj} {Exception}{NewLine}");
        //+:cnd:noEmit
#pragma warning restore CA1416 // Validate platform compatibility
    }

    private static LoggerConfiguration AddFileLogging(LoggerConfiguration configuration, string logFilePath)
    {
        //-:cnd:noEmit
#if __ANDROID__ || __IOS__ || NETSTANDARD
        return configuration
            .WriteTo.File(logFilePath, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fffzzz} [{Platform}] {Level:u1}/{SourceContext}: {Message:lj} {Exception}{NewLine}", fileSizeLimitBytes: 10485760) // 10mb
#if __ANDROID__
                .Enrich.WithProperty("Platform", "Android");
#elif __IOS__
            .Enrich.WithProperty("Platform", "iOS");
#else
                .Enrich.WithProperty("Platform", "WASM");
#endif
#else
            return configuration;
#endif
        //+:cnd:noEmit
    }

    private static string? GetLogFilePath(HostBuilderContext hostBuilderContext)
    {
        var logDirectory = hostBuilderContext.HostingEnvironment.GetAppDataPath();
        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            return null;
        }

        var assemblyName = Assembly.GetEntryAssembly()?.FullName ?? "unologging";
        return Path.Combine(logDirectory, $"{assemblyName}.log");
    }
}
