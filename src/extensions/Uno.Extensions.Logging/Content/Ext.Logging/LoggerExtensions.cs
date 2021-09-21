using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Logging
{
    public static class LoggerExtensions
    {
        private static bool? _logEnabledDebug;
        private static bool? _logEnabledTrace;
        private static bool? _logEnabledInformation;
        private static bool? _logEnabledWarning;
        private static bool? _logEnabledError;
        private static bool? _logEnabledCritical;

        public static void LazyLogDebug(this ILogger logger, Func<string> message, [CallerMemberName] string callerMethod = null)
        {
            if (Enabled(ref _logEnabledDebug, LogLevel.Debug, logger))
            {
                logger?.LogDebug($"{callerMethod} - {message?.Invoke()}");
            }
        }

        public static void LazyLogTrace(this ILogger logger, Func<string> message, [CallerMemberName] string callerMethod = null)
        {
            if (Enabled(ref _logEnabledTrace, LogLevel.Trace, logger))
            {
                logger?.LogTrace($"{callerMethod} - {message?.Invoke()}");
            }
        }

        public static void LazyLogInformation(this ILogger logger, Func<string> message, [CallerMemberName] string callerMethod = null)
        {
            if (Enabled(ref _logEnabledInformation, LogLevel.Information, logger))
            {
                logger?.LogInformation($"{callerMethod} - {message?.Invoke()}");
            }
        }

        public static void LazyLogWarning(this ILogger logger, Func<string> message, [CallerMemberName] string callerMethod = null)
        {
            if (Enabled(ref _logEnabledWarning, LogLevel.Warning, logger))
            {
                logger?.LogWarning($"{callerMethod} - {message?.Invoke()}");
            }
        }

        public static void LazyLogError(this ILogger logger, Func<string> message, [CallerMemberName] string callerMethod = null)
        {
            if (Enabled(ref _logEnabledError, LogLevel.Error, logger))
            {
                logger?.LogError($"{callerMethod} - {message?.Invoke()}");
            }
        }

        public static void LazyLogCritical(this ILogger logger, Func<string> message, [CallerMemberName] string callerMethod = null)
        {
            if (Enabled(ref _logEnabledCritical, LogLevel.Critical, logger))
            {
                logger?.LogCritical($"{callerMethod} - {message?.Invoke()}");
            }
        }

        private static bool Enabled(ref bool? enabled, LogLevel level, ILogger logger)
        {
            if (!enabled.HasValue)
            {
                enabled = logger?.IsEnabled(level);
            }

            return enabled ?? false;
        }
    }
}
