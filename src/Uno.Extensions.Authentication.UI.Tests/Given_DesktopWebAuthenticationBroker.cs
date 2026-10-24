// The desktop loopback broker only ever runs on Skia Desktop (TryRegister's runtime gate), and
// HttpListener is unavailable in the browser sandbox and pointless on the mobile heads - so this
// suite compiles only for the TFMs that can exercise it (net10.0 / net10.0-desktop).
#if !__ANDROID__ && !__IOS__ && !__WASM__ && !WINDOWS && !__MACCATALYST__
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.AuthenticationBroker;
using Uno.Foundation.Extensibility;
using Windows.Foundation;
using Windows.Security.Authentication.Web;

namespace Uno.Extensions.Authentication.UI.Tests;

/// <summary>
/// End-to-end coverage of <see cref="DesktopWebAuthenticationBrokerProvider"/>: the
/// loopback listener round trip, callback validation, cancellation, the registration with Uno's
/// static <see cref="WebAuthenticationBroker"/>, and the log output - the "browser" is an HTTP
/// GET against the callback, standing in for the IdP's post-sign-in redirect.
/// </summary>
/// <remarks>
/// Not parallelizable: the timeout and default-port cases set process-wide statics
/// (<see cref="WinRTFeatureConfiguration.WebAuthenticationBroker"/>, the broker's default port),
/// and the static-broker cases share one registered instance.
/// </remarks>
[TestClass]
[DoNotParallelize]
public class Given_DesktopWebAuthenticationBroker
{
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
		Task<HttpResponseMessage>? browser = null;
		broker.OnLaunch = (request, ct) =>
		{
			// Like a real browser, the GET only completes once the broker's listener answers it -
			// so it is started here and awaited after the broker returns, where a failure inside
			// it fails the test instead of vanishing.
			browser = http.GetAsync($"{callback}?code=stub-code&state=xyz", ct);
			return Task.CompletedTask;
		};
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback, cts.Token);

		var response = await browser!;
		response.EnsureSuccessStatusCode();
		response.Headers.CacheControl?.NoStore.Should().BeTrue("the completion page is served at a token-bearing URL and must not be cached");
		result.ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
		result.ResponseData.Should().Be($"{callback}?code=stub-code&state=xyz");
		broker.LaunchedRequestUri.Should().Be(RequestUri);
	}

	/// <summary>
	/// <c>response_mode=form_post</c>: the identity provider's auto-submitting form POSTs the
	/// response to the callback instead of redirecting with a query. Same result shape.
	/// </summary>
	[TestMethod]
	public async Task When_FormPostResponse_Then_CompletedFromBody()
	{
		var broker = new TestBroker();
		var callback = broker.GetCurrentApplicationCallbackUri();
		using var http = new HttpClient();
		// Owned by the test method, not the lambda: the POST is still in flight when the lambda
		// returns, so a using inside it would dispose the content mid-send.
		using var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["code"] = "stub-code", ["state"] = "xyz" });
		Task<HttpResponseMessage>? browser = null;
		broker.OnLaunch = (request, ct) =>
		{
			browser = http.PostAsync(callback, form, ct);
			return Task.CompletedTask;
		};
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback, cts.Token);

		(await browser!).EnsureSuccessStatusCode();
		result.ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
		result.ResponseData.Should().Be($"{callback}?code=stub-code&state=xyz",
			"a posted response must complete the flow in the same shape as a query-string one");
	}

	/// <summary>
	/// Any page in the system browser can POST to the loopback port (a urlencoded POST needs no
	/// CORS preflight). A body past the limit must be neither buffered nor allowed to end the
	/// flow: the real response, arriving afterwards, still completes it.
	/// </summary>
	[TestMethod]
	public async Task When_OversizedFormPost_Then_RejectedAndFlowStillCompletes()
	{
		var broker = new TestBroker();
		var callback = broker.GetCurrentApplicationCallbackUri();
		using var http = new HttpClient();
		using var oversized = new StringContent("junk=" + new string('x', 64 * 1024), System.Text.Encoding.ASCII, "application/x-www-form-urlencoded");
		Task? browser = null;
		broker.OnLaunch = (request, ct) =>
		{
			browser = Task.Run(async () =>
			{
				try
				{
					using var rejected = await http.PostAsync(callback, oversized, ct);
					rejected.IsSuccessStatusCode.Should().BeFalse("an oversized body is not a sign-in response");
				}
				catch (HttpRequestException)
				{
					// Equally a rejection: the listener answered without draining the body, and
					// the platform's HTTP stack reset the connection under the client.
				}

				(await http.GetAsync($"{callback}?code=stub-code", ct)).EnsureSuccessStatusCode();
			}, ct);
			return Task.CompletedTask;
		};
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback, cts.Token);

		await browser!;
		result.ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
		result.ResponseData.Should().Be($"{callback}?code=stub-code");
	}

	/// <summary>
	/// A double-clicked sign-in button: the second flow must fail with a message that says what
	/// happened, not with a bind error on the port the first flow holds - and must not disturb it.
	/// </summary>
	[TestMethod]
	public async Task When_FlowAlreadyInProgress_Then_SecondFlowRejectedAndFirstCompletes()
	{
		var broker = new TestBroker();
		var callback = broker.GetCurrentApplicationCallbackUri();
		using var http = new HttpClient();
		var launched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		broker.OnLaunch = (request, ct) =>
		{
			launched.TrySetResult();
			return Task.CompletedTask;
		};
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var first = broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback, cts.Token);
		await launched.Task.WaitAsync(cts.Token);

		Func<Task> second = () => broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback, cts.Token);
		await second.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already in progress*");

		(await http.GetAsync($"{callback}?code=stub-code", cts.Token)).EnsureSuccessStatusCode();
		(await first).ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
	}

	/// <summary>
	/// The default port is probed once and kept. If something else holds it when a flow starts,
	/// every later flow used to fail the same way until the app restarted.
	/// </summary>
	[TestMethod]
	public async Task When_DefaultPortTaken_Then_NextFlowPicksAnother()
	{
		var broker = new TestBroker();
		var taken = broker.GetCurrentApplicationCallbackUri();
		using var squatter = new System.Net.HttpListener();
		squatter.Prefixes.Add($"http://{taken.Host}:{taken.Port}/");
		squatter.Start();

		Func<Task> act = () => broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, taken, CancellationToken.None);
		await act.Should().ThrowAsync<System.Net.HttpListenerException>();

		broker.GetCurrentApplicationCallbackUri().Port.Should().NotBe(taken.Port,
			"a default port that would not bind must not be handed out again");
	}

	[TestMethod]
	public async Task When_RequestUriNotHttp_Then_Throws()
	{
		var broker = new TestBroker();

		Func<Task> act = () => broker.AuthenticateAsync(
			WebAuthenticationOptions.None,
			new Uri("file:///etc/passwd"),
			broker.GetCurrentApplicationCallbackUri(),
			CancellationToken.None);

		await act.Should().ThrowAsync<ArgumentException>(
			"only a web URL may be handed to the OS's URL handler, which dispatches on scheme");
		broker.LaunchedRequestUri.Should().BeNull("nothing must be launched for a rejected request URI");
	}

	/// <summary>
	/// WinRT has no timeout status, so the broker's own AuthenticationTimeout is reported as
	/// UserCancel - marked by the error detail, which is how callers tell the two apart.
	/// </summary>
	[TestMethod]
	public async Task When_BrokerTimesOut_Then_UserCancelWithTimeoutDetail()
	{
		var broker = new TestBroker();
		var callback = broker.GetCurrentApplicationCallbackUri();
		using var _ = ShortAuthenticationTimeout();

		// The fake browser never redirects; only the broker's own timeout can end this.
		var result = await broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback, CancellationToken.None);

		result.ResponseStatus.Should().Be(WebAuthenticationStatus.UserCancel);
		result.ResponseErrorDetail.Should().Be(DesktopWebAuthenticationBrokerProvider.TimeoutErrorDetail,
			"the detail is the only thing distinguishing a timeout from a cancel");
	}

	/// <summary>
	/// OAuth responses that ride the URL fragment (implicit flows)
	/// never reach an HTTP listener - browsers do not send fragments to servers. The broker must
	/// answer a bare callback request with a relay page whose script re-requests the callback
	/// with the fragment as the query, and hand the result back as a fragment.
	/// </summary>
	[TestMethod]
	public async Task When_FragmentResponse_Then_RelayedAndReturned()
	{
		var broker = new TestBroker();
		var callback = broker.GetCurrentApplicationCallbackUri();
		using var http = new HttpClient();
		Task? browser = null;
		broker.OnLaunch = (request, ct) =>
		{
			browser = Task.Run(async () =>
			{
				// The "browser" lands on the callback with a fragment, which never reaches the
				// listener: the first GET arrives bare and must serve the relay page...
				var relay = await http.GetStringAsync(callback, ct);
				relay.Should().Contain("location.replace", "a bare callback hit must serve the fragment relay page");

				// ...whose script re-requests the callback with the fragment content as query.
				(await http.GetAsync($"{callback}?uno-fragment=1&id_token=stub-id-token&scope=openid", ct)).EnsureSuccessStatusCode();
			}, ct);
			return Task.CompletedTask;
		};
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback, cts.Token);

		await browser!;
		result.ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
		result.ResponseData.Should().Be($"{callback}#id_token=stub-id-token&scope=openid",
			"the relayed fragment must come back in its original response shape");
	}

	/// <summary>
	/// A redirect with neither query nor fragment (a bare logout
	/// callback) must still complete rather than loop on the relay page.
	/// </summary>
	[TestMethod]
	public async Task When_NoQueryAndNoFragment_Then_CompletesEmpty()
	{
		var broker = new TestBroker();
		var callback = broker.GetCurrentApplicationCallbackUri();
		using var http = new HttpClient();
		Task? browser = null;
		broker.OnLaunch = (request, ct) =>
		{
			browser = Task.Run(async () =>
			{
				// Bare hit: relay page comes back; its script finds no fragment and re-requests
				// with the no-fragment sentinel.
				await http.GetStringAsync(callback, ct);
				(await http.GetAsync($"{callback}?uno-no-fragment=1", ct)).EnsureSuccessStatusCode();
			}, ct);
			return Task.CompletedTask;
		};
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback, cts.Token);

		await browser!;
		result.ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
		result.ResponseData.Should().Be(callback.OriginalString,
			"the sentinel must be stripped - the caller sees a clean, parameterless callback");
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

	/// <summary>
	/// Token-leak guard (AGENTS.md §5): every response shape plus the port-in-use and timeout
	/// paths, with secrets in the code, the posted body and the relayed fragment - none of it may
	/// reach the log at any level. The announcement line proves the capture is wired.
	/// </summary>
	[TestMethod]
	public async Task When_ResponsesCarrySecrets_Then_NoneLogged()
	{
		var broker = new TestBroker();
		var callback = broker.GetCurrentApplicationCallbackUri();
		using var http = new HttpClient();
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		await CompleteAsync(broker, callback, (request, ct) => http.GetAsync($"{callback}?code=secret-code&access_token=secret-access&state=xyz", ct), cts.Token);
		await CompleteAsync(broker, callback, async (request, ct) =>
		{
			await http.GetStringAsync(callback, ct);
			await http.GetAsync($"{callback}?uno-fragment=1&id_token=secret-id-token", ct);
		}, cts.Token);
		using var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["code"] = "secret-posted-code" });
		await CompleteAsync(broker, callback, (request, ct) => http.PostAsync(callback, form, ct), cts.Token);

		// The remaining paths must not be answered: the fake browser goes back to never navigating.
		broker.Reset();

		using (var squatter = new System.Net.HttpListener())
		{
			squatter.Prefixes.Add($"http://{callback.Host}:{callback.Port}/");
			squatter.Start();
			Func<Task> act = () => broker.AuthenticateAsync(WebAuthenticationOptions.None, new Uri($"{RequestUri}&login_hint=secret-hint"), callback, cts.Token);
			await act.Should().ThrowAsync<System.Net.HttpListenerException>();
		}

		using (ShortAuthenticationTimeout())
		{
			await broker.AuthenticateAsync(WebAuthenticationOptions.None, new Uri($"{RequestUri}&login_hint=secret-hint"), callback, cts.Token);
		}

		var logs = broker.Logs.Text;
		logs.Should().Contain("desktop loopback broker", "the capture must have seen the broker's own log lines");
		logs.Should().Contain("Error:", "the port-in-use path must have logged");
		logs.Should().Contain("Warning:", "the timeout path must have logged");
		logs.Should().NotContain("secret", "no code, token or request parameter may appear in the log");
	}

	/// <summary>
	/// First registration wins (Uno's registry is TryAdd): an app that registered its own provider
	/// keeps it, <c>TryRegister</c> says so, and the registry still hands out the app's instance.
	/// The positive case - the loopback broker registering on an empty registry - cannot be
	/// observed in a process that needs the test double registered first.
	/// </summary>
	[TestMethod]
	public void When_ProviderRegisteredFirst_Then_TryRegisterKeepsIt()
	{
		var shared = StaticBrokerRegistration.EnsureRegistered();

		var registered = DesktopWebAuthenticationBrokerProvider.TryRegister();

		registered.Should().BeFalse("another provider was registered first and must keep winning");
		ApiExtensibility.CreateInstance<IWebAuthenticationBrokerProvider>(null!, out var provider).Should().BeTrue();
		provider.Should().BeSameAs(shared);
	}

	/// <summary>
	/// The static <see cref="WebAuthenticationBroker"/> resolves its provider once and keeps it:
	/// callback and sign-in both reach the registered broker.
	/// </summary>
	[TestMethod]
	public async Task When_AuthenticateThroughStaticBroker_Then_CallbackQueryReturned()
	{
		var shared = StaticBrokerRegistration.EnsureRegistered();
		var callback = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
		callback.Should().Be(shared.GetCurrentApplicationCallbackUri(), "the static broker must delegate to the registered provider");
		callback.IsLoopback.Should().BeTrue();
		callback.Scheme.Should().Be(Uri.UriSchemeHttp);
		using var http = new HttpClient();
		Task<HttpResponseMessage>? browser = null;
		shared.OnLaunch = (request, ct) =>
		{
			browser = http.GetAsync($"{callback}?code=stub-code", ct);
			return Task.CompletedTask;
		};
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback).AsTask(cts.Token);

		(await browser!).EnsureSuccessStatusCode();
		result.ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
		result.ResponseData.Should().Be($"{callback}?code=stub-code");
		shared.LaunchedRequestUri.Should().Be(RequestUri);
	}

	/// <summary>
	/// Through the static broker the timeout detail survives (Uno adds no timeout of its own that
	/// would preempt it), and the one-flow-at-a-time guard is live because there is one instance.
	/// </summary>
	[TestMethod]
	public async Task When_StaticBrokerTimesOut_Then_UserCancelWithTimeoutDetail()
	{
		var shared = StaticBrokerRegistration.EnsureRegistered();
		var callback = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
		using var _ = ShortAuthenticationTimeout();

		var result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback).AsTask();

		result.ResponseStatus.Should().Be(WebAuthenticationStatus.UserCancel);
		result.ResponseErrorDetail.Should().Be(DesktopWebAuthenticationBrokerProvider.TimeoutErrorDetail);
		shared.LaunchedRequestUri.Should().Be(RequestUri);
	}

	[TestMethod]
	public async Task When_SecondFlowThroughStaticBroker_Then_RejectedAndFirstCompletes()
	{
		var shared = StaticBrokerRegistration.EnsureRegistered();
		var callback = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
		using var http = new HttpClient();
		var launched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		shared.OnLaunch = (request, ct) =>
		{
			launched.TrySetResult();
			return Task.CompletedTask;
		};
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var first = WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback).AsTask(cts.Token);
		await launched.Task.WaitAsync(cts.Token);

		Func<Task> second = () => WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback).AsTask(cts.Token);
		await second.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already in progress*");

		(await http.GetAsync($"{callback}?code=stub-code", cts.Token)).EnsureSuccessStatusCode();
		(await first).ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
	}

	[TestMethod]
	public async Task When_CancelledThroughStaticBroker_Then_TaskCancels()
	{
		StaticBrokerRegistration.EnsureRegistered();
		var callback = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

		Func<Task> act = () => WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback).AsTask(cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	/// <summary>Runs one flow to completion, with <paramref name="browser"/> standing in for the redirect.</summary>
	private static async Task CompleteAsync(TestBroker broker, Uri callback, Func<Uri, CancellationToken, Task> browser, CancellationToken ct)
	{
		Task? redirect = null;
		broker.OnLaunch = (request, token) =>
		{
			redirect = Task.Run(() => browser(request, token), token);
			return Task.CompletedTask;
		};

		var result = await broker.AuthenticateAsync(WebAuthenticationOptions.None, RequestUri, callback, ct);

		await redirect!;
		result.ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
	}

	/// <summary>
	/// Sets the broker's process-wide AuthenticationTimeout to 200 ms until disposed: long enough
	/// for a listener to start, short enough not to stretch the lane. The timeout is a global
	/// Uno setting, so it cannot be injected per instance.
	/// </summary>
	private static IDisposable ShortAuthenticationTimeout()
	{
		var original = WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout;
		WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout = TimeSpan.FromMilliseconds(200);
		return new Restore(() => WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout = original);
	}

	private sealed class Restore(Action restore) : IDisposable
	{
		public void Dispose() => restore();
	}
}
#endif
