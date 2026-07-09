using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Tests.Core.Logging;

/// <summary>
/// Guards that the Reactive logging helpers do not permanently snapshot the first host's logging
/// provider / ambient factory.
/// </summary>
/// <remarks>
/// A snapshot pins that host's whole <c>ServiceProvider</c> for the process lifetime. In a downstream
/// host that loads previewed apps into their own collectible <c>AssemblyLoadContext</c>s the first
/// previewed app's ALC would then never collect. The <c>Log&lt;T&gt;</c> loggers must forward to the
/// currently-configured provider, and <c>Reset</c> must drop it.
/// </remarks>
[TestClass]
public class Given_LogExtensions
{
	[TestCleanup]
	public void Cleanup() => LogExtensions.Reset();

	[TestMethod]
	public void When_ProviderChanges_Then_ForwardingLoggerUsesLatest()
	{
		// The cached Log<T>() logger is obtained once...
		var logger = LogExtensions.Log<Given_LogExtensions>();

		var first = new RecordingProvider();
		LogExtensions.SetProvider(first);
		logger.LogInformation("one");

		// ...then the provider is swapped (as it would be for a second host).
		var second = new RecordingProvider();
		LogExtensions.SetProvider(second);
		logger.LogInformation("two");

		first.Messages.Should().ContainSingle().Which.Should().Be("one");
		second.Messages.Should().ContainSingle().Which.Should().Be("two",
			"the forwarding logger must resolve the current provider, not a snapshot of the first");
	}

	[TestMethod]
	public void When_Reset_Then_ProviderIsReleased()
	{
		var provider = new RecordingProvider();
		LogExtensions.SetProvider(provider);

		LogExtensions.Reset();

		// After reset the forwarder no longer routes to the released provider.
		LogExtensions.Log<Given_LogExtensions>().LogInformation("after-reset");
		provider.Messages.Should().BeEmpty("Reset must drop the previously-configured provider");
	}

	private sealed class RecordingProvider : ILoggerProvider
	{
		public List<string> Messages { get; } = new();

		public ILogger CreateLogger(string categoryName) => new RecordingLogger(Messages);

		public void Dispose() { }

		private sealed class RecordingLogger : ILogger
		{
			private readonly List<string> _messages;

			public RecordingLogger(List<string> messages) => _messages = messages;

			public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

			public bool IsEnabled(LogLevel logLevel) => true;

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
				=> _messages.Add(formatter(state, exception));
		}
	}
}
