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

	/// <summary>
	/// Both timeouts default to five minutes and the OIDC client's <see cref="BrowserOptions.Timeout"/>
	/// starts first, so in production it is the one that elapses: the client must still see a
	/// timeout, not an unknown error.
	/// </summary>
	[TestMethod]
	public async Task When_OidcTimeoutElapsesFirst_Then_TimeoutResult()
	{
		StaticBrokerRegistration.EnsureRegistered();
		var callback = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();

		// The broker's AuthenticationTimeout stays at its default; only the client's can end this.
		var result = await new WebAuthenticatorBrowser().InvokeAsync(
			new BrowserOptions(RequestUri.OriginalString, callback.OriginalString) { Timeout = TimeSpan.FromMilliseconds(200) });

		result.ResultType.Should().Be(BrowserResultType.Timeout, "the client's own timeout must not be reported as an unknown error");
		result.Response.Should().BeNull();
	}

	/// <summary>
	/// The caller's cancellation is theirs to observe. Reported as a result it would read as a
	/// failed sign-in - and a cancelled sign-out would return <c>true</c> and clear the tokens
	/// with the cancelled token, which is the one path the authentication service cannot guard.
	/// </summary>
	[TestMethod]
	public async Task When_CallerCancels_Then_CancellationPropagates()
	{
		StaticBrokerRegistration.EnsureRegistered();
		var callback = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

		Func<Task> act = () => new WebAuthenticatorBrowser().InvokeAsync(new BrowserOptions(RequestUri.OriginalString, callback.OriginalString), cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	/// <summary>
	/// Uno's built-in broker throws <see cref="NotImplementedException"/> on Skia Desktop, and the
	/// static <see cref="WebAuthenticationBroker"/> keeps it when it was touched before
	/// <c>AddOidc()</c> registered the loopback one. The error must name the remedy rather than
	/// surface as a bare "not implemented".
	/// </summary>
	[TestMethod]
	public async Task When_BrokerNotImplemented_Then_ErrorNamesRegistrationOrder()
	{
		var shared = StaticBrokerRegistration.EnsureRegistered();
		var callback = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
		shared.OnLaunch = (_, _) => throw new NotImplementedException();

		var result = await new WebAuthenticatorBrowser().InvokeAsync(new BrowserOptions(RequestUri.OriginalString, callback.OriginalString));

		result.ResultType.Should().Be(BrowserResultType.UnknownError);
		result.Error.Should().Contain("TryRegister").And.Contain("AddOidc", "the error must say that the broker was used before the loopback one was registered");
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
