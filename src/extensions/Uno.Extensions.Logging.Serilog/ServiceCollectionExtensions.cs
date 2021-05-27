﻿using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Uno.Extensions;

namespace Uno.Extensions.Logging.Serilog
{
    public static class ServiceCollectionExtensions
    {
        public static IHostBuilder UseSerilog(this IHostBuilder hostBuilder,
            bool consoleLoggingEnabled = false,
            bool fileLoggineEnabled = false,
            bool isAppLogging = true)
        {
            return hostBuilder.UseSerilog(() => consoleLoggingEnabled, () => fileLoggineEnabled, isAppLogging);
        }

        public static IHostBuilder UseSerilog(this IHostBuilder hostBuilder,
            Func<bool> consoleLoggingEnabled = null,
            Func<bool> fileLoggineEnabled = null,
            bool isAppLogging = true)
        {
            return hostBuilder.ConfigureLogging((context, loggingBuilder) =>
            {
                var loggerConfiguration = new LoggerConfiguration();

                loggerConfiguration.ReadFrom.Configuration(context.Configuration);

                if (consoleLoggingEnabled?.Invoke() ?? false)
                {
                    AddConsoleLogging(loggerConfiguration);
                }

                if (fileLoggineEnabled?.Invoke() ?? false)
                {
                    AddFileLogging(loggerConfiguration, GetLogFilePath(isAppLogging));
                }

                var logger = loggerConfiguration.CreateLogger();

                if (isAppLogging)
                {
                    // The logs coming from Uno will be sent to the app logger and not the host logger.
                    LogExtensionPoint.AmbientLoggerFactory.AddSerilog(logger);
                }

                loggingBuilder.AddSerilog(logger);
            });
        }

        private static LoggerConfiguration AddConsoleLogging(LoggerConfiguration configuration)
        {
            return configuration
                //-:cnd:noEmit
#if __ANDROID__
                .WriteTo.AndroidLog(outputTemplate: "{Message:lj} {Exception}{NewLine}");
#elif __IOS__
                .WriteTo.NSLog(outputTemplate: "{Level:u1}/{SourceContext}: {Message:lj} {Exception}");
#else
                .WriteTo.Debug(outputTemplate: "{Timestamp:MM-dd HH:mm:ss.fffzzz} {Level:u1}/{SourceContext}: {Message:lj} {Exception}{NewLine}");
#endif
            //+:cnd:noEmit
        }

        private static LoggerConfiguration AddFileLogging(LoggerConfiguration configuration, string logFilePath)
        {
            //-:cnd:noEmit
#if __ANDROID__ || __IOS__
            return configuration
                .WriteTo.File(logFilePath, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fffzzz} [{Platform}] {Level:u1}/{SourceContext}: {Message:lj} {Exception}{NewLine}", fileSizeLimitBytes: 10485760) // 10mb
#if __ANDROID__
                .Enrich.WithProperty("Platform", "Android");
#elif __IOS__
                .Enrich.WithProperty("Platform", "iOS");
#endif
#else
            return configuration;
#endif
            //+:cnd:noEmit
        }

        private static string GetLogFilePath(bool isAppLogging = true)
        {
            var logDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var assemblyName = Assembly.GetEntryAssembly()?.FullName ?? "unologging";
            return isAppLogging
                ? Path.Combine(logDirectory, $"{assemblyName}.log")
                : Path.Combine(logDirectory, $"{assemblyName}.host.log");
        }
    }
}
