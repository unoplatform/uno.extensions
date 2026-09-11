using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Authentication.UI.Tests;

/// <summary>
/// Collects everything written through <see cref="ILogger"/> so a test can assert on log content -
/// specifically that no token material reaches it.
/// </summary>
/// <remarks>
/// Deliberately captures the formatted message and the exception text, since a leak is as likely to
/// come from an interpolated exception message as from a log template.
/// </remarks>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
	private readonly StringBuilder _builder = new();
	private readonly object _gate = new();

	/// <summary>Everything logged so far.</summary>
	public string Text
	{
		get
		{
			lock (_gate)
			{
				return _builder.ToString();
			}
		}
	}

	public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

	public void Dispose()
	{
	}

	private void Append(string categoryName, LogLevel logLevel, string message, Exception? exception)
	{
		lock (_gate)
		{
			_builder.Append(logLevel).Append(' ').Append(categoryName).Append(": ").AppendLine(message);
			if (exception is not null)
			{
				_builder.AppendLine(exception.ToString());
			}
		}
	}

	private sealed class CapturingLogger : ILogger
	{
		private readonly CapturingLoggerProvider _owner;
		private readonly string _categoryName;

		public CapturingLogger(CapturingLoggerProvider owner, string categoryName)
		{
			_owner = owner;
			_categoryName = categoryName;
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
			=> _owner.Append(_categoryName, logLevel, formatter(state, exception), exception);
	}
}
