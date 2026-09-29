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

	// The ambient Uno logger factory is resolved on demand and re-resolved after `Reset`, rather than
	// bound once for the process lifetime. Caching the FIRST host's factory permanently pinned that
	// host's whole `ServiceProvider` — and in a downstream host that loads previewed apps into their
	// own collectible AssemblyLoadContexts, the first previewed app's ALC — forever. The `Log<T>`
	// loggers below are forwarders that re-resolve on each call, so they never snapshot a stale factory.
	private static ILoggerFactory? _unoLogger;
	private static bool _boundToUnoLogger;

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

	/// <summary>
	/// Drops the cached provider and ambient logger factory so the next <see cref="CreateLog(string)"/>
	/// re-resolves. Intended to be called on host shutdown so the previous host's logger factory (and its
	/// service provider) is not retained.
	/// </summary>
	public static void Reset()
	{
		_provider = null;
		_unoLogger = null;
		_boundToUnoLogger = false;
	}

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
		// A forwarding logger that re-resolves the concrete logger on each call instead of snapshotting
		// one at first touch. A snapshot would capture (and pin) the ambient factory of whichever host
		// happened to be active when this generic was first used.
		public static ILogger Logger { get; } = new ForwardingLogger(typeof(T).FullName!);
	}

	/// <summary>
	/// An <see cref="ILogger"/> that resolves the underlying logger via <see cref="CreateLog(string)"/> on
	/// every call, so it always uses the current provider / ambient factory and never retains a stale one.
	/// </summary>
	private sealed class ForwardingLogger : ILogger
	{
		private readonly string _categoryName;

		public ForwardingLogger(string categoryName)
			=> _categoryName = categoryName;

		public IDisposable? BeginScope<TState>(TState state)
			where TState : notnull
			=> CreateLog(_categoryName).BeginScope(state);

		public bool IsEnabled(LogLevel logLevel)
			=> CreateLog(_categoryName).IsEnabled(logLevel);

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
			=> CreateLog(_categoryName).Log(logLevel, eventId, state, exception, formatter);
	}
}
