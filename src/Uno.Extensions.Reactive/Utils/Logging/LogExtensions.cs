#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Uno.Extensions.Reactive.Logging;

internal static class LogExtensions
{
	private static ILoggerProvider? _provider;
	private static bool _boundToUnoLogger;
	private static ILoggerFactory? _unoLogger;

	private static ILoggerFactory? FindUnoAmbientLogger()
	{
		if (!_boundToUnoLogger)
		{
			_boundToUnoLogger = true;
			try
			{
				_unoLogger = Type
					.GetType("Uno.Extensions.LogExtensionPoint, Uno.Core.Extensions.Logging.Singleton", throwOnError: false)
					?.GetProperty("AmbientLoggerFactory", BindingFlags.Public | BindingFlags.Static)
					?.GetValue(null) as ILoggerFactory;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine($"Failed to bind to uno ambient logger: {e}");
			}
		}

		return _unoLogger;
	}

	public static void SetProvider(ILoggerProvider provider)
		=> _provider = provider;

	public static ILogger CreateLog(string categoryName)
		=> _provider?.CreateLogger(categoryName)
			?? FindUnoAmbientLogger()?.CreateLogger(categoryName)
			?? NullLogger.Instance;

	public static ILogger CreateLog(this Type type)
		=> CreateLog(type.FullName ?? type.ToString());

	public static ILogger Log<T>()
		=> Holder<T>.Logger;

	public static ILogger Log<T>(this T owner)
		=> Holder<T>.Logger;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Trace(this ILogger logger, string message)
		=> logger.LogTrace(0, message);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Debug(this ILogger logger, string message)
		=> logger.LogDebug(0, message);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Info(this ILogger logger, string message)
		=> logger.LogInformation(0, message);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Warn(this ILogger logger, string message)
		=> logger.LogWarning(0, message);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Warn(this ILogger logger, Exception error, string message)
		=> logger.LogWarning(0, error, message);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Error(this ILogger logger, Exception error, string message)
		=> logger.LogError(0, error, message);

	private static class Holder<T>
	{
		public static ILogger Logger { get; } = CreateLog(typeof(T).FullName!);
	}
}
