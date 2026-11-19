using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Aspire.Tests;

internal sealed class RecordingLogger<T> : ILogger<T>
{
	private readonly List<string> _messages = new();

	public IReadOnlyList<string> Messages => _messages;

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
		_messages.Add(formatter(state, exception));
}
