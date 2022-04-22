using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Logging;

public static class LoggerExtensions
{
	private const int CallerNameFixedWidth = 50;

	public static void LogDebugMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
	{
		logger?.LogDebug(FormatLogText(callerMethod, message));
	}

	public static void LogTraceMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
	{
		logger?.LogTrace(FormatLogText(callerMethod, message));
	}

	public static void LogInformationMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
	{
		logger?.LogInformation(FormatLogText(callerMethod, message));
	}

	public static void LogWarningMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
	{
		logger?.LogWarning(FormatLogText(callerMethod, message));
	}

	public static void LogErrorMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
	{
		logger?.LogError(FormatLogText(callerMethod, message));
	}

	public static void LogCriticalMessage(this ILogger logger, string message, [CallerMemberName] string callerMethod = "")
	{
		logger?.LogCritical(FormatLogText(callerMethod, message));
	}

	private static string FormatLogText(string callerMethod, string message) => $"{callerMethod.PadRight(CallerNameFixedWidth)} - {message}";
}
