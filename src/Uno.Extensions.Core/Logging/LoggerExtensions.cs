using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Logging;
// BUG: This Extensions used with a untyped ILogger and INavigator from Uno.Extensions.Navigation did cause Issue https://github.com/unoplatform/uno.extensions/issues/2708
// TODO: Check if there should be added also extensions each for a typed ILogger or if Navigator is really causing this issue

/// <summary>
/// Provides extension methods for logging.
/// </summary>
public static class LoggerExtensions
{
    private const int CallerNameFixedWidth = 50;

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogDebugMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogDebug(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs a debug message using a typed logger.
    /// </summary>
    /// <typeparam name="T">The type associated with the logger.</typeparam>
    /// <param name="logger">The typed logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogDebugMessage<T>(this ILogger<T> logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogDebug(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs a trace message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogTraceMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogTrace(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs a trace message using a typed logger.
    /// </summary>
    /// <typeparam name="T">The type associated with the logger.</typeparam>
    /// <param name="logger">The typed logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogTraceMessage<T>(this ILogger<T> logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogTrace(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogInformationMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogInformation(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs an informational message using a typed logger.
    /// </summary>
    /// <typeparam name="T">The type associated with the logger.</typeparam>
    /// <param name="logger">The typed logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogInformationMessage<T>(this ILogger<T> logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogInformation(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogWarningMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogWarning(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs a warning message using a typed logger.
    /// </summary>
    /// <typeparam name="T">The type associated with the logger.</typeparam>
    /// <param name="logger">The typed logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogWarningMessage<T>(this ILogger<T> logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogWarning(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogErrorMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogError(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs an error message using a typed logger.
    /// </summary>
    /// <typeparam name="T">The type associated with the logger.</typeparam>
    /// <param name="logger">The typed logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogErrorMessage<T>(this ILogger<T> logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogError(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs an error message with an exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogErrorMessage(this ILogger logger, Exception ex, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogError(ex, FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs an error message with an exception using a typed logger.
    /// </summary>
    /// <typeparam name="T">The type associated with the logger.</typeparam>
    /// <param name="logger">The typed logger instance.</param>
    /// <param name="ex">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogErrorMessage<T>(this ILogger<T> logger, Exception ex, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogError(ex, FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs a critical message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogCriticalMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogCritical(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Logs a critical message using a typed logger.
    /// </summary>
    /// <typeparam name="T">The type associated with the logger.</typeparam>
    /// <param name="logger">The typed logger instance.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerMethod">The name of the calling method.</param>
    public static void LogCriticalMessage<T>(this ILogger<T> logger, string message, [CallerMemberName] string callerMethod = "")
    {
        logger?.LogCritical(FormatLogText(callerMethod, message));
    }

    /// <summary>
    /// Formats the log text with the caller method and message.
    /// </summary>
    /// <param name="callerMethod">The name of the calling method.</param>
    /// <param name="message">The message to log.</param>
    /// <returns>The formatted log text.</returns>
    private static string FormatLogText(string callerMethod, string message) => $"{callerMethod} - {message}";
}
