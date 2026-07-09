using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Guards that the static <see cref="Region.Logger"/> is released on navigation-host shutdown so the
/// last host's logger (and the service provider behind it) is not retained. In a downstream host that
/// loads previewed apps into their own collectible AssemblyLoadContexts, a retained logger pins the
/// app's ALC.
/// </summary>
[TestClass]
public class Given_Region
{
	[TestCleanup]
	public void Cleanup() => Region.ResetLogger(Region.Logger);

	[TestMethod]
	public void When_ResetLogger_WithMatchingInstance_Then_LoggerFallsBackToNull()
	{
		var logger = new NullLoggerFactory().CreateLogger("test");
		Region.Logger = logger;
		Region.Logger.Should().BeSameAs(logger);

		Region.ResetLogger(logger);

		Region.Logger.Should().NotBeSameAs(logger, "the matching logger must be cleared on shutdown");
		Region.Logger.Should().BeOfType<NullLogger<NavigationRegion>>("Region falls back to the null logger");
	}

	[TestMethod]
	public void When_ResetLogger_WithDifferentInstance_Then_CurrentLoggerKept()
	{
		var running = new NullLoggerFactory().CreateLogger("running-host");
		Region.Logger = running;

		// A different (already stopped) host tries to reset — it must not clobber the running host's logger.
		var stopped = new NullLoggerFactory().CreateLogger("stopped-host");
		Region.ResetLogger(stopped);

		Region.Logger.Should().BeSameAs(running, "a non-matching reset must leave the current logger untouched");
	}
}
