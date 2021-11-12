using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Uno.Extensions.Reactive.Utils.Logging;

internal static class LogExtensions
{
	private static ILoggerProvider? _provider;

	public static void SetProvider(ILoggerProvider provider)
		=> _provider = provider;

	public static ILogger Log<T>(this T owner)
		=> Holder<T>.Logger;

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
		public static ILogger Logger { get; } = _provider?.CreateLogger(typeof(T).FullName) ?? NullLogger.Instance;
	}
}
