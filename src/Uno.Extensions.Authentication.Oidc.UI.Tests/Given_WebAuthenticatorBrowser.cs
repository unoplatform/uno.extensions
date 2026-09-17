// WinAppSDK drives WinUIEx instead of WebAuthenticationBroker, so there is no broker to stub there.
#if !WINDOWS
using System;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.OidcClient.Browser;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Authentication.UI.Tests;
using Uno.UI.RuntimeTests;
using Windows.Security.Authentication.Web;

namespace Uno.Extensions.Authentication.Oidc.UI.Tests;

/// <summary>
/// The real <see cref="WebAuthenticatorBrowser"/> over a stubbed <see cref="WebAuthenticationBroker"/>:
/// how each broker outcome reaches OidcClient. <see cref="Given_OidcAuthentication"/> replaces the
/// adapter with <see cref="StubBrowser"/>, so nothing there notices if this mapping changes.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_WebAuthenticatorBrowser
{
	private static readonly BrowserOptions Options = new("https://stub-idp.example/connect/authorize?state=abc", "web-tests://callback");

	private static StubWebAuthenticationBroker Broker()
	{
		StubWebAuthenticationBroker.EnsureRegistered();
		var broker = StubWebAuthenticationBroker.Instance;
		broker.Reset();
		return broker;
	}

	[TestMethod]
	public async Task When_BrokerSucceeds_Then_ResponseIsTheCallbackUrl()
	{
		var broker = Broker();
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await new WebAuthenticatorBrowser().InvokeAsync(Options, cts.Token);

		result.ResultType.Should().Be(BrowserResultType.Success);
		result.Response.Should().StartWith(Options.EndUrl).And.Contain("state=abc");
		broker.InvocationCount.Should().Be(1);
	}

	/// <summary>
	/// <c>Error</c> stays null so OidcClient names the result type itself - which is what lets the
	/// provider log a cancel as information rather than as a failure.
	/// </summary>
	[TestMethod]
	public async Task When_BrokerReportsUserCancel_Then_UserCancelWithNoErrorText()
	{
		Broker().NextStatus = WebAuthenticationStatus.UserCancel;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await new WebAuthenticatorBrowser().InvokeAsync(Options, cts.Token);

		result.ResultType.Should().Be(BrowserResultType.UserCancel);
		result.Error.Should().BeNull();
	}

	[TestMethod]
	public async Task When_BrokerReportsItsTimeout_Then_Timeout()
	{
		var broker = Broker();
		broker.NextStatus = WebAuthenticationStatus.UserCancel;
		broker.NextErrorDetail = DesktopWebAuthenticationBrokerProvider.TimeoutErrorDetail;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await new WebAuthenticatorBrowser().InvokeAsync(Options, cts.Token);

		result.ResultType.Should().Be(BrowserResultType.Timeout);
	}

	[TestMethod]
	public async Task When_BrokerReportsHttpError_Then_HttpErrorNamingTheStatus()
	{
		Broker().NextStatus = WebAuthenticationStatus.ErrorHttp;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await new WebAuthenticatorBrowser().InvokeAsync(Options, cts.Token);

		result.ResultType.Should().Be(BrowserResultType.HttpError);
		result.Error.Should().Contain(nameof(WebAuthenticationStatus.ErrorHttp));
	}
}
#endif
