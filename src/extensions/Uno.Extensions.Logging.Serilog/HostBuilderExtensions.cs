﻿using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Uno.Extensions.Logging.Serilog
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseSerilog(this IHostBuilder hostBuilder,
            bool consoleLoggingEnabled = false,
            bool fileLoggingEnabled = false,
            Action<LoggerConfiguration> configureLogger = null)
        {
            return hostBuilder.UseSerilog(() => consoleLoggingEnabled, () => fileLoggingEnabled, configureLogger);
        }

        public static IHostBuilder UseSerilog(this IHostBuilder hostBuilder,
            Func<bool> consoleLoggingEnabled,
            Func<bool> fileLoggingEnabled,
            Action<LoggerConfiguration> configureLogger = null)
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
                            AddFileLogging(loggerConfiguration, GetLogFilePath(context));
                        }

                        configureLogger?.Invoke(loggerConfiguration);

                        var logger = loggerConfiguration.CreateLogger();

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

        private static string GetLogFilePath(HostBuilderContext hostBuilderContext)
        {
            var logDirectory = hostBuilderContext.HostingEnvironment.ContentRootPath;
            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                return null;
            }

            var assemblyName = Assembly.GetEntryAssembly()?.FullName ?? "unologging";
            return Path.Combine(logDirectory, $"{assemblyName}.log");
        }
    }
}
