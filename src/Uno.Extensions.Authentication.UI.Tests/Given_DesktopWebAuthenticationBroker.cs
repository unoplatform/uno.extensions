// The desktop loopback broker only ever runs on Skia Desktop (TryRegister's runtime gate), and
// HttpListener is unavailable in the browser sandbox and pointless on the mobile heads - so this
// suite compiles only for the TFMs that can exercise it (net9.0 / net9.0-desktop).
#if !__ANDROID__ && !__IOS__ && !__WASM__ && !WINDOWS && !__MACCATALYST__
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Security.Authentication.Web;

namespace Uno.Extensions.Authentication.UI.Tests;

/// <summary>
/// End-to-end coverage of <see cref="DesktopWebAuthenticationBrokerProvider"/> (spec 012 F8): the
/// loopback listener round trip, callback validation, and cancellation - the "browser" is an HTTP
/// GET against the callback, standing in for the IdP's post-sign-in redirect.
/// </summary>
[TestClass]
public class Given_DesktopWebAuthenticationBroker
{
	private sealed class TestBroker : DesktopWebAuthenticationBrokerProvider
	{
		/// <summary>The fake browser; defaults to never navigating anywhere.</summary>
		public Func<Uri, CancellationToken, Task> OnLaunch { get; set; } = (_, _) => Task.CompletedTask;

		public Uri? LaunchedRequestUri { get; private set; }

		protected override Task LaunchBrowserAsync(Uri requestUri, CancellationToken ct)
		{
			LaunchedRequestUri = requestUri;
			return OnLaunch(requestUri, ct);
		}
	}

	private static readonly Uri RequestUri = new("https://stub-idp.example/authorize?client_id=x");

	[TestMethod]
	public void When_DefaultCallback_Then_LoopbackHttpAndStable()
	{
		var broker = new TestBroker();

		var first = broker.GetCurrentApplicationCallbackUri();
		var second = broker.GetCurrentApplicationCallbackUri();

		first.IsLoopback.Should().BeTrue();
		first.Scheme.Should().Be(Uri.UriSchemeHttp);
		second.Should().Be(first, "login and logout must share the same redirect URI");
	}

	[TestMethod]
	public async Task When_Authenticate_Then_CallbackQueryReturned()
	{
		var broker = new TestBroker();
		var callback = broker.GetCurrentApplicationCallbackUri();
		using var http = new HttpClient();
		broker.OnLaunch = (request, ct) =>
		{
			// Fire-and-forget like a real browser: the GET's response only arrives once the
			// broker's listener answers it, so awaiting here would deadlock the flow.
			_ = http.GetAsync($"{callback}?code=stub-code&state=xyz", ct);
			return Task.CompletedTask;
		};
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback, cts.Token);

		result.ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
		result.ResponseData.Should().Be($"{callback}?code=stub-code&state=xyz");
		broker.LaunchedRequestUri.Should().Be(RequestUri);
	}

	[TestMethod]
	public async Task When_CallbackNotLoopback_Then_Throws()
	{
		var broker = new TestBroker();

		Func<Task> act = () => broker.AuthenticateAsync(
			WebAuthenticationOptions.None,
			RequestUri,
			new Uri("https://example.com/callback"),
			CancellationToken.None);

		await act.Should().ThrowAsync<ArgumentException>(
			"the broker must refuse to listen on anything but loopback HTTP");
		broker.LaunchedRequestUri.Should().BeNull("the browser must not open for a rejected callback");
	}

	[TestMethod]
	public async Task When_Cancelled_Then_CancellationPropagates()
	{
		var broker = new TestBroker();
		var callback = broker.GetCurrentApplicationCallbackUri();
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

		// The fake browser never completes the redirect, so only the caller's token can end this.
		Func<Task> act = () => broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback, cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
#endif
