using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Authentication;

/// <summary>
/// Records every message at every level so a test can assert on what a logging pipeline would
/// have seen. Trace is the worst case for leaks, so nothing is filtered.
/// </summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
	private readonly StringBuilder _text = new();

	public string Text => _text.ToString();

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
		_text.Append(logLevel).Append(' ').AppendLine(formatter(state, exception));
}
