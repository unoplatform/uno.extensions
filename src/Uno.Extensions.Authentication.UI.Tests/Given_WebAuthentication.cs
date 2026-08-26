using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Authentication;
using Uno.Extensions.Hosting;
using Uno.UI.RuntimeTests;
using Windows.Security.Authentication.Web;

namespace Uno.Extensions.Authentication.UI.Tests;

/// <summary>
/// End-to-end coverage of the web provider against <see cref="StubWebAuthenticationBroker"/>:
/// sign-in, sign-out and their cancellation/failure modes, with no browser and no network.
/// </summary>
/// <remarks>
/// Lives in a UI test project rather than the plain unit-test one for the same reason as the MSAL
/// and OIDC suites: Uno.Extensions.Authentication.WinUI can't load in a bare test host - its
/// Uno.WinUI module initializer needs a live Uno.UI.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class Given_WebAuthentication
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

	private const string LoginStartUri = "https://stub-idp.example/authorize";
	private const string CallbackUri = "web-tests://callback";

	private sealed record Harness(
		IHost Host,
		StubWebAuthenticationBroker Broker,
		IAuthenticationService Authentication,
		ITokenCache Tokens,
		CapturingLoggerProvider Logs) : IDisposable
	{
		public void Dispose() => Host.Dispose();
	}

	/// <summary>
	/// Builds a host wired to the stub broker, resets the broker's per-test state, and clears any
	/// tokens left behind by an earlier test - the token cache's key-value storage is shared by
	/// every test in the run and, on desktop, survives across runs.
	/// </summary>
	private static async Task<Harness> CreateHarnessAsync()
	{
		var harness = CreateHarness();

		using var purge = new CancellationTokenSource(Timeout);
		await harness.Tokens.ClearAsync(purge.Token);

		return harness;
	}

	private static Harness CreateHarness()
	{
		StubWebAuthenticationBroker.EnsureRegistered();
		var broker = StubWebAuthenticationBroker.Instance;
		broker.Reset();
		var logs = new CapturingLoggerProvider();

		var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_WebAuthentication).Assembly)
			.UseAuthentication(auth => auth
				.AddWeb(web => web
					.LoginStartUri(LoginStartUri)
					.LoginCallbackUri(CallbackUri)
					.LogoutStartUri("https://stub-idp.example/logout")
					.LogoutCallbackUri(CallbackUri)))
			.ConfigureServices(services => services
				.AddLogging(logging => logging
					.SetMinimumLevel(LogLevel.Trace)
					.AddProvider(logs)))
			.Build();

		return new Harness(
			host,
			broker,
			host.Services.GetRequiredService<IAuthenticationService>(),
			host.Services.GetRequiredService<ITokenCache>(),
			logs);
	}

	private static CancellationTokenSource Cts() => new(Timeout);

	[TestMethod]
	public async Task When_Login_Then_AccessTokenCached()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		var result = await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);

		harness.Broker.InvocationCount.Should().Be(1, "diagnostics: {0}", harness.Logs.Text);
		result.Should().BeTrue("diagnostics: {0}", harness.Logs.Text);
		harness.Broker.LastRequestUri!.OriginalString.Should().StartWith(LoginStartUri);

		var tokens = await harness.Tokens.GetAsync(cts.Token);
		tokens.Should().ContainKey(TokenCacheExtensions.AccessTokenKey);
		tokens[TokenCacheExtensions.AccessTokenKey].Should().Be(harness.Broker.LastAccessToken);
		tokens.Should().ContainKey(TokenCacheExtensions.RefreshTokenKey);
	}

	/// <summary>
	/// Red test for spec 012 F5: the provider used to ignore
	/// <see cref="WebAuthenticationResult.ResponseStatus"/> and return an empty (non-null) token
	/// dictionary on cancel, which <c>TokenCache.SaveAsync</c> turns into a wipe of the previously
	/// cached session. Cancellation must surface as <see cref="OperationCanceledException"/> before
	/// any save - the same contract as the MSAL provider.
	/// </summary>
	[TestMethod]
	public async Task When_LoginCancelled_Then_PreviousSessionSurvives()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);
		harness.Broker.NextStatus = WebAuthenticationStatus.UserCancel;

		Func<Task> act = () => harness.Authentication.LoginAsync(default, cancellationToken: cts.Token).AsTask();

		await act.Should().ThrowAsync<OperationCanceledException>(
			"backing out of the sign-in UI is a cancellation, not a failed login");
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeTrue(
			"a cancelled re-login must not wipe the session the user still has");
	}

	/// <summary>
	/// Spec 012 F5, error branch: an HTTP error from the interactive flow is a failed login - no
	/// exception, no tokens.
	/// </summary>
	[TestMethod]
	public async Task When_LoginFails_Then_NotAuthenticated()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		harness.Broker.NextStatus = WebAuthenticationStatus.ErrorHttp;

		var result = await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);

		result.Should().BeFalse();
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeFalse();
	}

	[TestMethod]
	public async Task When_Logout_Then_TokensCleared()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);

		var loggedOut = await harness.Authentication.LogoutAsync(default, cts.Token);

		loggedOut.Should().BeTrue();
		harness.Broker.InvocationCount.Should().Be(2, "logout drives the end-session flow through the broker");
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeFalse();
	}

	/// <summary>
	/// Red test for spec 012 F6: the provider used to discard the broker result on logout and
	/// return true unconditionally, so a cancelled end-session flow still flushed the local token
	/// cache and reported the sign-out as successful.
	/// </summary>
	[TestMethod]
	public async Task When_LogoutCancelled_Then_StillAuthenticated()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);
		harness.Broker.NextStatus = WebAuthenticationStatus.UserCancel;

		var loggedOut = await harness.Authentication.LogoutAsync(default, cts.Token);

		loggedOut.Should().BeFalse("a cancelled sign-out must not report success");
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeTrue(
			"tokens must survive a sign-out the user backed out of");
	}

	/// <summary>
	/// A service the typed AddWeb callbacks receive, standing in for an app's own OAuth client
	/// (the shape the Authentication.WebExtensionsDemo sample uses): the login URI is prepared at
	/// sign-in time and the "code" on the redirect is exchanged for tokens in PostLogin.
	/// </summary>
	private sealed class FakeOAuthService
	{
		public const string AuthorizeUri = "https://stub-idp.example/authorize?client_id=fake";
		public const string ExchangedAccessToken = "typed-exchanged-access-token";

		public int ExchangeCount { get; private set; }
		public string? LastRedirectUri { get; private set; }

		public string BuildAuthorizeUri() => AuthorizeUri;

		public string CallbackUri => "web-tests://callback";

		public Task<IDictionary<string, string>?> ExchangeCodeAsync(string redirectUri, CancellationToken ct)
		{
			ExchangeCount++;
			LastRedirectUri = redirectUri;
			return Task.FromResult<IDictionary<string, string>?>(new Dictionary<string, string>
			{
				[TokenCacheExtensions.AccessTokenKey] = ExchangedAccessToken,
			});
		}
	}

	/// <summary>
	/// Repro for the sample-app configuration (Authentication.WebExtensionsDemo): everything is
	/// supplied through AddWeb&lt;TService&gt;'s typed callbacks - no static LoginStartUri - and the
	/// tokens come from PostLogin, not the redirect query.
	/// </summary>
	[TestMethod]
	public async Task When_TypedCallbacksOnly_Then_LoginUsesThem()
	{
		StubWebAuthenticationBroker.EnsureRegistered();
		var broker = StubWebAuthenticationBroker.Instance;
		broker.Reset();
		var logs = new CapturingLoggerProvider();

		using var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_WebAuthentication).Assembly)
			.UseAuthentication(auth => auth
				.AddWeb<FakeOAuthService>(web => web
					.PrepareLoginStartUri(async (oauth, services, cache, credentials, loginStartUri, ct) =>
						oauth.BuildAuthorizeUri())
					.PrepareLoginCallbackUri(async (oauth, services, cache, credentials, loginCallbackUri, ct) =>
						oauth.CallbackUri)
					.PostLogin(async (oauth, services, cache, credentials, redirectUri, tokens, ct) =>
						await oauth.ExchangeCodeAsync(redirectUri, ct))))
			.ConfigureServices(services => services
				.AddSingleton<FakeOAuthService>()
				.AddLogging(logging => logging
					.SetMinimumLevel(LogLevel.Trace)
					.AddProvider(logs)))
			.Build();

		var authentication = host.Services.GetRequiredService<IAuthenticationService>();
		var tokens = host.Services.GetRequiredService<ITokenCache>();
		var oauth = host.Services.GetRequiredService<FakeOAuthService>();
		using var purge = Cts();
		await tokens.ClearAsync(purge.Token);
		using var cts = Cts();

		var result = await authentication.LoginAsync(default, cancellationToken: cts.Token);

		broker.InvocationCount.Should().Be(1, "diagnostics: {0}", logs.Text);
		broker.LastRequestUri!.OriginalString.Should().Be(FakeOAuthService.AuthorizeUri,
			"the login URI must come from the typed PrepareLoginStartUri callback");
		oauth.ExchangeCount.Should().Be(1, "the typed PostLogin callback must run");
		result.Should().BeTrue("diagnostics: {0}", logs.Text);
		(await tokens.GetAsync(cts.Token))[TokenCacheExtensions.AccessTokenKey]
			.Should().Be(FakeOAuthService.ExchangedAccessToken, "PostLogin's tokens must be the ones cached");
	}

	/// <summary>
	/// Red test for spec 014: the literal <c>{RedirectUri}</c> token in LoginStartUri must be
	/// replaced with the URL-encoded effective callback, and with no LoginCallbackUri configured
	/// the callback must come from <c>WebAuthenticationBroker.GetCurrentApplicationCallbackUri()</c>
	/// - the platform-correct value, no per-platform configuration or callbacks needed.
	/// </summary>
	[TestMethod]
	public async Task When_RedirectUriPlaceholder_Then_BrokerCallbackSubstituted()
	{
		StubWebAuthenticationBroker.EnsureRegistered();
		var broker = StubWebAuthenticationBroker.Instance;
		broker.Reset();

		using var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_WebAuthentication).Assembly)
			.UseAuthentication(auth => auth
				.AddWeb(web => web
					// No LoginCallbackUri anywhere: the provider must derive it from the broker
					// and substitute it into the start URI.
					.LoginStartUri($"{LoginStartUri}?client_id=demo&redirect_uri={{RedirectUri}}")))
			.Build();

		var authentication = host.Services.GetRequiredService<IAuthenticationService>();
		var tokens = host.Services.GetRequiredService<ITokenCache>();
		using var purge = Cts();
		await tokens.ClearAsync(purge.Token);
		using var cts = Cts();

		var result = await authentication.LoginAsync(default, cancellationToken: cts.Token);

		result.Should().BeTrue();
		var derivedCallback = broker.GetCurrentApplicationCallbackUri().OriginalString;
		broker.LastCallbackUri!.OriginalString.Should().Be(derivedCallback,
			"the broker's own callback must be used when none is configured");
		broker.LastRequestUri!.OriginalString.Should().Be(
			$"{LoginStartUri}?client_id=demo&redirect_uri={Uri.EscapeDataString(derivedCallback)}",
			"the placeholder must reach the identity provider as the URL-encoded callback");
	}

	/// <summary>
	/// Spec 014, fallback without a placeholder: a start URI carrying no redirect at all used to
	/// fail with "LoginCallbackUri not specified"; the broker-derived default now applies.
	/// </summary>
	[TestMethod]
	public async Task When_NoCallbackConfigured_Then_BrokerCallbackUsed()
	{
		StubWebAuthenticationBroker.EnsureRegistered();
		var broker = StubWebAuthenticationBroker.Instance;
		broker.Reset();

		using var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_WebAuthentication).Assembly)
			.UseAuthentication(auth => auth
				.AddWeb(web => web
					.LoginStartUri(LoginStartUri)))
			.Build();

		var authentication = host.Services.GetRequiredService<IAuthenticationService>();
		var tokens = host.Services.GetRequiredService<ITokenCache>();
		using var purge = Cts();
		await tokens.ClearAsync(purge.Token);
		using var cts = Cts();

		var result = await authentication.LoginAsync(default, cancellationToken: cts.Token);

		result.Should().BeTrue();
		broker.LastCallbackUri!.OriginalString.Should().Be(
			broker.GetCurrentApplicationCallbackUri().OriginalString);
	}

	/// <summary>
	/// Spec 014 diagnostics: when nothing configures a callback and the broker cannot derive one
	/// either, the warning must name the broker and carry its reason. The message this replaced
	/// said only that LoginCallbackUri and redirect_uri were missing - which reads as a
	/// configuration slip even when the start URI carries {RedirectUri} and the real cause is a
	/// platform with no callback to derive (an iOS app with no CFBundleURLTypes entry, say).
	/// </summary>
	[TestMethod]
	public async Task When_BrokerCannotDeriveCallback_Then_WarningNamesBroker()
	{
		StubWebAuthenticationBroker.EnsureRegistered();
		var broker = StubWebAuthenticationBroker.Instance;
		broker.Reset();
		broker.CallbackUriError = "No custom scheme found for this application.";
		var logs = new CapturingLoggerProvider();

		using var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_WebAuthentication).Assembly)
			.UseAuthentication(auth => auth
				.AddWeb(web => web
					.LoginStartUri($"{LoginStartUri}?client_id=demo&redirect_uri={{RedirectUri}}")))
			.ConfigureServices(services => services
				.AddLogging(logging => logging
					.SetMinimumLevel(LogLevel.Trace)
					.AddProvider(logs)))
			.Build();

		var authentication = host.Services.GetRequiredService<IAuthenticationService>();
		var tokens = host.Services.GetRequiredService<ITokenCache>();
		using var purge = Cts();
		await tokens.ClearAsync(purge.Token);
		using var cts = Cts();

		var result = await authentication.LoginAsync(default, cancellationToken: cts.Token);

		result.Should().BeFalse("no callback means no flow to start");
		broker.InvocationCount.Should().Be(0, "the sign-in UI must not open without a callback");
		logs.Text.Should().Contain("WebAuthenticationBroker",
			"the warning must name the source that failed, not just the two configuration settings");
		logs.Text.Should().Contain(broker.CallbackUriError,
			"the broker's own reason is what tells the developer what to fix");
	}

	/// <summary>
	/// Red test for spec 012 F4: no <see cref="CancellationToken"/> used to reach the broker call,
	/// so an already-cancelled login still drove the whole interactive flow to completion.
	/// </summary>
	[TestMethod]
	public async Task When_LoginAlreadyCancelled_Then_NoPrompt()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		Func<Task> act = () => harness.Authentication.LoginAsync(default, cancellationToken: cts.Token).AsTask();

		await act.Should().ThrowAsync<OperationCanceledException>();
		harness.Broker.InvocationCount.Should().Be(0, "a cancelled login must not open the sign-in UI");
	}
}
