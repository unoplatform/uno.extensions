#if !__ANDROID__ && !__IOS__ && !__WASM__ && !WINDOWS && !__MACCATALYST__
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;

namespace Uno.Extensions.Authentication.UI.Tests;

/// <summary>
/// <c>AddWeb</c>/<c>AddOidc</c> register the desktop broker at builder time, when no logger exists,
/// so how that went is logged through the host's logger once the host is built. In this process
/// the suite's double is registered first, which is the outcome the line exists to explain.
/// </summary>
[TestClass]
[DoNotParallelize]
public class Given_DesktopWebAuthenticationBrokerRegistration
{
	private const string NotUsedLine = "the desktop loopback broker is not used";

	[TestMethod]
	public void When_AnotherProviderRegisteredFirst_Then_LoggedOnceThroughHostLogger()
	{
		StaticBrokerRegistration.EnsureRegistered();
		var logs = new CapturingLogger();

		using var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_DesktopWebAuthenticationBrokerRegistration).Assembly)
			.ConfigureLogging(logging => logging.ClearProviders().AddProvider(new CapturingLoggerProvider(logs)))
			.UseAuthentication(auth => auth.AddOidc().AddWeb())
			.Build();

		logs.Text.Should().Contain(NotUsedLine, "the outcome must reach the host's logger; at builder time there is none");
		logs.Text.Split(NotUsedLine).Length.Should().Be(2, "one line per host, however many providers registered the broker");
	}
}
#endif
