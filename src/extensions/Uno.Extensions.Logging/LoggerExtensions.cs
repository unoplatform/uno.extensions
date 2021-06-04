using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Logging
{
    public static class LoggerExtensions
    {
        private static bool? _debugLogEnabled;

        public static void LazyLogDebug(this ILogger logger, Func<string> message, [CallerMemberName] string callerMethod = null)
        {
            if (!_debugLogEnabled.HasValue)
            {
                _debugLogEnabled = logger?.IsEnabled(LogLevel.Debug);
            }

            if (_debugLogEnabled.Value)
            {
                logger?.LogDebug($"{callerMethod} - {message?.Invoke()}");
            }
        }
    }
}
