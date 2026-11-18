using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Aspire.Hosting.Tests;

internal sealed class ListLogger : ILogger
{
	public List<(LogLevel Level, string Message)> Entries { get; } = [];

	public IDisposable? BeginScope<TState>(TState state)
		where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
		Entries.Add((logLevel, formatter(state, exception)));
}
