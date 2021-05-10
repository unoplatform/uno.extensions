using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Hosting;
using Serilog;
using Uno.Extensions;

namespace ApplicationTemplate
{
	/// <summary>
	/// This class is used for logging configuration.
	/// - Configures the logger filters (see appsettings.json).
	/// - Configures the loggers.
	/// </summary>
	public static class LoggingConfiguration
	{
		/// <summary>
		/// Adds logging to the host.
		/// This is used to get the logs before the app host is ready.
		/// </summary>
		/// <param name="hostBuilder">Host builder.</param>
		/// <returns><see cref="IHostBuilder"/>.</returns>
		public static IHostBuilder AddHostLogging(this IHostBuilder hostBuilder)
			=> hostBuilder.AddLogging(isAppLogging: false);

		/// <summary>
		/// Adds logging to the app host.
		/// </summary>
		/// <param name="hostBuilder">Host builder.</param>
		/// <returns><see cref="IHostBuilder"/>.</returns>
		public static IHostBuilder AddAppLogging(this IHostBuilder hostBuilder)
			=> hostBuilder.AddLogging(isAppLogging: true);

		private static IHostBuilder AddLogging(this IHostBuilder hostBuilder, bool isAppLogging)
		{
			return hostBuilder.ConfigureLogging((context, loggingBuilder) =>
			{
				var loggerConfiguration = new LoggerConfiguration();

				loggerConfiguration.ReadFrom.Configuration(context.Configuration);

				if (ConsoleLogging.GetIsEnabled())
				{
					AddConsoleLogging(loggerConfiguration);
				}

				if (FileLogging.GetIsEnabled())
				{
					AddFileLogging(loggerConfiguration, FileLogging.GetLogFilePath(isAppLogging));
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

		public static class ConsoleLogging
		{
			public static bool GetIsEnabled()
			{
//-:cnd:noEmit
#if DEBUG
//+:cnd:noEmit
				var defaultValue = true;
//-:cnd:noEmit
#else
//+:cnd:noEmit
				var defaultValue = false;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

				return ConfigurationSettings.GetIsSettingEnabled("console-logging", defaultValue);
			}

			public static void SetIsEnabled(bool isEnabled)
			{
				ConfigurationSettings.SetIsSettingEnabled("console-logging", isEnabled);
			}
		}

		public static class FileLogging
		{
			public static bool GetIsEnabled() => ConfigurationSettings.GetIsSettingEnabled("file-logging", defaultValue: false);

			public static void SetIsEnabled(bool isEnabled) => ConfigurationSettings.SetIsSettingEnabled("file-logging", isEnabled);

			public static void DeleteLogFiles()
			{
				foreach (var path in GetLogFilePaths())
				{
					File.Delete(path);
				}
			}

			public static string GetLogFilePath(bool isAppLogging = true)
			{
				var logDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

				return isAppLogging
					? Path.Combine(logDirectory, "ApplicationTemplate.log")
					: Path.Combine(logDirectory, "ApplicationTemplate.host.log");
			}

			public static string[] GetLogFilePaths()
			{
				return new[]
				{
					GetLogFilePath(),
					GetLogFilePath(isAppLogging: false),
				};
			}
		}
	}
}
