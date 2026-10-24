#if !__ANDROID__ && !__IOS__ && !__WASM__ && !WINDOWS && !__MACCATALYST__
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.OidcClient.Browser;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Authentication.Oidc;
using Windows.Security.Authentication.Web;

namespace Uno.Extensions.Authentication.UI.Tests;

/// <summary>
/// The OIDC provider's <see cref="WebAuthenticatorBrowser"/> over the desktop loopback broker:
/// the broker's status and <see cref="DesktopWebAuthenticationBrokerProvider.TimeoutErrorDetail"/>
/// must reach the OIDC client as a result type rather than be dropped.
/// </summary>
[TestClass]
[DoNotParallelize]
public class Given_WebAuthenticatorBrowser
{
	private static readonly Uri RequestUri = new("https://stub-idp.example/authorize?client_id=x");

	[TestMethod]
	public async Task When_BrokerSucceeds_Then_SuccessWithResponse()
	{
		var shared = StaticBrokerRegistration.EnsureRegistered();
		var callback = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
		using var http = new HttpClient();
		Task<HttpResponseMessage>? redirect = null;
		shared.OnLaunch = (request, ct) =>
		{
			redirect = http.GetAsync($"{callback}?code=stub-code&state=xyz", ct);
			return Task.CompletedTask;
		};
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await new WebAuthenticatorBrowser().InvokeAsync(new BrowserOptions(RequestUri.OriginalString, callback.OriginalString), cts.Token);

		(await redirect!).EnsureSuccessStatusCode();
		result.ResultType.Should().Be(BrowserResultType.Success);
		result.Response.Should().Be($"{callback}?code=stub-code&state=xyz");
	}

	[TestMethod]
	public async Task When_BrokerTimesOut_Then_TimeoutResult()
	{
		StaticBrokerRegistration.EnsureRegistered();
		var callback = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
		var original = WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout;
		WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout = TimeSpan.FromMilliseconds(200);
		try
		{
			// The fake browser never redirects; the broker's own timeout ends the flow, ahead of
			// the OIDC client's default five-minute BrowserOptions.Timeout.
			var result = await new WebAuthenticatorBrowser().InvokeAsync(new BrowserOptions(RequestUri.OriginalString, callback.OriginalString));

			result.ResultType.Should().Be(BrowserResultType.Timeout, "the broker's timeout detail must not be reported as a plain cancel or an unknown error");
			result.Response.Should().BeNull();
		}
		finally
		{
			WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout = original;
		}
	}

	[TestMethod]
	public void When_UserCancelWithoutTimeoutDetail_Then_UserCancelResult()
	{
		var result = WebAuthenticatorBrowser.ToBrowserResult(new WebAuthenticationResult(null, 0, WebAuthenticationStatus.UserCancel));

		result.ResultType.Should().Be(BrowserResultType.UserCancel);
	}

	[TestMethod]
	public void When_ErrorHttp_Then_HttpErrorResult()
	{
		var result = WebAuthenticatorBrowser.ToBrowserResult(new WebAuthenticationResult(null, 503, WebAuthenticationStatus.ErrorHttp));

		result.ResultType.Should().Be(BrowserResultType.HttpError);
		result.Error.Should().Contain("503");
	}
}
#endif
