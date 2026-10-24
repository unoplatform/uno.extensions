#if !__ANDROID__ && !__IOS__ && !__WASM__ && !WINDOWS && !__MACCATALYST__
using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Authentication.UI.Tests;

/// <summary>
/// Collects everything written through <see cref="ILogger"/>, at every level, so a test can
/// assert on log content - specifically that no token material reaches it.
/// </summary>
/// <remarks>
/// Captures the formatted message and the exception text: a leak is as likely to come from an
/// interpolated exception message as from a log template.
/// </remarks>
internal sealed class CapturingLogger : ILogger
{
	private readonly StringBuilder _text = new();
	private readonly object _gate = new();

	/// <summary>Everything logged so far.</summary>
	public string Text
	{
		get
		{
			lock (_gate)
			{
				return _text.ToString();
			}
		}
	}

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		lock (_gate)
		{
			_text.Append(logLevel).Append(": ").AppendLine(formatter(state, exception));
			if (exception is not null)
			{
				_text.AppendLine(exception.ToString());
			}
		}
	}
}
#endif
