using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Records every log entry at every level so tests can assert on what navigation logs.
/// </summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
	public ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

	public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

	public void Dispose()
	{
	}

	private sealed class CapturingLogger : ILogger
	{
		private readonly CapturingLoggerProvider _owner;

		public CapturingLogger(CapturingLoggerProvider owner)
		{
			_owner = owner;
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
			=> _owner.Entries.Enqueue((logLevel, formatter(state, exception), exception));
	}
}
