using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.OidcClient.Browser;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Authentication;
using Uno.Extensions.Authentication.UI.Tests;
using Uno.Extensions.Hosting;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Authentication.Oidc.UI.Tests;

/// <summary>
/// End-to-end coverage of the OIDC provider against <see cref="StubOidcServer"/>: sign-in, silent
/// refresh and its failure modes, with no network and no human at a sign-in prompt.
/// </summary>
/// <remarks>
/// Lives in a UI test project rather than the plain unit-test one for the same reason as the MSAL
/// suite: Uno.Extensions.Authentication.Oidc.WinUI can't load in a bare test host - its Uno.WinUI
/// module initializer needs a live Uno.UI.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class Given_OidcAuthentication
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

	private sealed record Harness(
		IHost Host,
		StubOidcServer Server,
		StubBrowser Browser,
		IAuthenticationService Authentication,
		ITokenCache Tokens,
		CapturingLoggerProvider Logs) : IDisposable
	{
		public void Dispose() => Host.Dispose();
	}

	/// <summary>
	/// Builds a host wired to the stub provider, then clears any tokens left behind by an earlier
	/// test so each test starts signed out - the token cache's key-value storage is shared by every
	/// test in the run and, on desktop, survives across runs.
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
		var server = new StubOidcServer();
		var browser = new StubBrowser();
		var logs = new CapturingLoggerProvider();

		var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_OidcAuthentication).Assembly)
			.UseAuthentication(auth => auth
				.AddOidc(oidc => oidc
					.ConfigureOidcClientOptions(options =>
					{
						options.Authority = StubOidcServer.Issuer;
						options.ClientId = StubOidcServer.ClientId;
						options.Scope = "openid profile offline_access";
						options.RedirectUri = "oidc-tests://callback";
						options.PostLogoutRedirectUri = "oidc-tests://callback";
						// Canned metadata: no discovery round trip, which removes a network-shaped
						// failure mode from mobile CI - same reasoning as the MSAL suite's
						// WithInstanceDiscovery(false).
						options.ProviderInformation = server.ProviderInformation;
						// The stub mints no id_token (the code flow does not require one), and no
						// signature validator is configured, so validation must be opted out.
						options.Policy.RequireIdentityTokenSignature = false;
						options.LoadProfile = false;
						options.HttpClientFactory = _ => server.HttpClient;
					})))
			// Last registration wins: replaces the WebAuthenticationBroker-backed browser that
			// AddOidc registers, so no window ever opens.
			.ConfigureServices(services => services
				.AddSingleton<IBrowser>(browser)
				// Trace is the worst case for token leaks, so everything is captured.
				.AddLogging(logging => logging
					.SetMinimumLevel(LogLevel.Trace)
					.AddProvider(logs)))
			.Build();

		return new Harness(
			host,
			server,
			browser,
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

		result.Should().BeTrue();
		harness.Browser.WasInvoked.Should().BeTrue("the first sign-in has no tokens to go silent with");
		harness.Server.TokenRequestCount.Should().Be(1);

		var tokens = await harness.Tokens.GetAsync(cts.Token);
		tokens.Should().ContainKey(TokenCacheExtensions.AccessTokenKey);
		tokens[TokenCacheExtensions.AccessTokenKey].Should().Be(harness.Server.LastAccessToken);
		tokens.Should().ContainKey(TokenCacheExtensions.RefreshTokenKey);
	}

	[TestMethod]
	public async Task When_Refresh_Then_TokenRenewed()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);
		var firstToken = (await harness.Tokens.GetAsync(cts.Token))[TokenCacheExtensions.AccessTokenKey];

		var refreshed = await harness.Authentication.RefreshAsync(cts.Token);

		refreshed.Should().BeTrue();
		harness.Server.RefreshRequestCount.Should().Be(1);
		var renewedToken = (await harness.Tokens.GetAsync(cts.Token))[TokenCacheExtensions.AccessTokenKey];
		renewedToken.Should().Be(harness.Server.LastAccessToken);
		renewedToken.Should().NotBe(firstToken, "OIDC refresh always redeems the refresh token for a new access token");
	}

	/// <summary>
	/// Red test for spec 017 F1: the provider used to ignore <c>RefreshTokenResult.IsError</c> and
	/// cache the (null) access token from the failed response, leaving the user looking
	/// authenticated with a dead token - the OIDC mirror of MSAL fix b3a3e3093.
	/// </summary>
	[TestMethod]
	public async Task When_RefreshRejected_Then_NotAuthenticated()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);
		harness.Server.RefreshError = "invalid_grant";

		var refreshed = await harness.Authentication.RefreshAsync(cts.Token);

		refreshed.Should().BeFalse("a rejected refresh must not report the user as still authenticated");
		harness.Server.RefreshRequestCount.Should().Be(1);
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeFalse();
		(await harness.Tokens.GetAsync(cts.Token)).Should().NotContainKey(
			TokenCacheExtensions.AccessTokenKey,
			"a failed refresh must not leave a dead access token behind");
	}

	/// <summary>
	/// Identity providers reject refresh tokens with codes of their own, outside RFC 6749 §5.2
	/// (Entra's <c>interaction_required</c>, for one). Treating an unlisted code as a network blip
	/// keeps the user "authenticated" on a refresh token the provider has already refused.
	/// </summary>
	[TestMethod]
	public async Task When_RefreshRejectedWithProviderSpecificCode_Then_NotAuthenticated()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);
		harness.Server.RefreshError = "interaction_required";

		var refreshed = await harness.Authentication.RefreshAsync(cts.Token);

		refreshed.Should().BeFalse("the token endpoint answered, and the answer was no");
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeFalse();
	}

	/// <summary>
	/// The one OAuth answer that is not a verdict on the refresh token: the server asking to be
	/// tried again later. Same outcome as not reaching it at all.
	/// </summary>
	[TestMethod]
	public async Task When_RefreshTemporarilyUnavailable_Then_SessionKept()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);
		harness.Server.RefreshError = "temporarily_unavailable";

		var refreshed = await harness.Authentication.RefreshAsync(cts.Token);

		refreshed.Should().BeTrue("the previous tokens are still the session");
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeTrue();
	}

	/// <summary>
	/// Red test for spec 017 F11: the provider never passed the cached id_token as the
	/// end-session <c>id_token_hint</c>. Without it the identity provider cannot trust the
	/// post-logout redirect, so it prompts for confirmation and never redirects back to the app -
	/// on desktop the loopback listener then waits until the broker timeout with the UI stuck.
	/// </summary>
	[TestMethod]
	public async Task When_Logout_Then_EndSessionCarriesIdTokenHint()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);

		// The stub token endpoint mints no id_token; plant one the way a real IdP would have.
		var tokens = new Dictionary<string, string>(await harness.Tokens.GetAsync(cts.Token))
		{
			[TokenCacheExtensions.IdTokenKey] = "stub-id-token",
		};
		await harness.Tokens.SaveAsync(await harness.Tokens.GetCurrentProviderAsync(cts.Token) ?? "Oidc", tokens, cts.Token);

		var loggedOut = await harness.Authentication.LogoutAsync(default, cts.Token);

		loggedOut.Should().BeTrue();
		harness.Browser.LastStartUrl.Should().Contain("id_token_hint=stub-id-token",
			"without the hint the identity provider cannot trust the post-logout redirect and never returns to the app");
	}

	[TestMethod]
	public async Task When_Logout_Then_TokensCleared()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);

		var loggedOut = await harness.Authentication.LogoutAsync(default, cts.Token);

		loggedOut.Should().BeTrue();
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeFalse();
	}

	/// <summary>
	/// Red test for spec 017 F2: the provider used to ignore <c>LogoutResult.IsError</c> and return
	/// true unconditionally, so a cancelled end-session flow still flushed the local token cache and
	/// reported the sign-out as successful.
	/// </summary>
	[TestMethod]
	public async Task When_LogoutCancelled_Then_StillAuthenticated()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);
		harness.Browser.NextResultType = Duende.IdentityModel.OidcClient.Browser.BrowserResultType.UserCancel;

		var loggedOut = await harness.Authentication.LogoutAsync(default, cts.Token);

		loggedOut.Should().BeFalse("a cancelled sign-out must not report success");
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeTrue(
			"tokens must survive a sign-out the user backed out of");
	}

	/// <summary>
	/// Red test, the Oidc mirror of the Web provider's F5 case: the browser adapter used to report
	/// a cancelled sign-in as a plain error, so the provider returned null and AuthenticationService
	/// cleared the session the user still had.
	/// </summary>
	[TestMethod]
	public async Task When_LoginCancelled_Then_PreviousSessionSurvives()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);
		var session = harness.Server.LastAccessToken;
		harness.Browser.NextResultType = BrowserResultType.UserCancel;

		var result = await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);

		result.Should().BeFalse("backing out of the sign-in UI is a login that did not happen");
		(await harness.Tokens.GetAsync(cts.Token))[TokenCacheExtensions.AccessTokenKey].Should().Be(session,
			"a cancelled re-login must not wipe the session the user still has");
	}

	/// <summary>
	/// The same rule when the browser reports anything else - a timeout, an HTTP error, or an
	/// app-supplied <see cref="IBrowser"/> that puts its own text in <c>Error</c> on a cancel: the
	/// session must not depend on how the failure was worded.
	/// </summary>
	[TestMethod]
	public async Task When_ReLoginFails_Then_PreviousSessionSurvives()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);
		var session = harness.Server.LastAccessToken;
		harness.Browser.NextResultType = BrowserResultType.Timeout;

		var result = await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);

		result.Should().BeFalse();
		(await harness.Tokens.GetAsync(cts.Token))[TokenCacheExtensions.AccessTokenKey].Should().Be(session);
	}

	/// <summary>
	/// A refresh that never reached a verdict - the token endpoint unreachable - says nothing about
	/// the session, so signing an offline user out at startup is the wrong answer (the MSAL
	/// provider's rule). Only the identity provider rejecting the refresh token ends the session.
	/// </summary>
	[TestMethod]
	public async Task When_RefreshFailsOffline_Then_SessionKept()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);
		var issued = harness.Server.LastAccessToken;
		harness.Server.RefreshUnavailable = true;

		var refreshed = await harness.Authentication.RefreshAsync(cts.Token);

		refreshed.Should().BeTrue("the previous tokens are still the session");
		(await harness.Tokens.GetAsync(cts.Token))[TokenCacheExtensions.AccessTokenKey].Should().Be(issued,
			"the cached tokens must stand until the identity provider actually rejects them");
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeTrue();
	}

	/// <summary>
	/// AGENTS.md section 7: tokens must never reach Uno.Extensions log output - asserted on a
	/// Trace-level capture across sign-in and refresh, the two paths that handle every token.
	/// </summary>
	[TestMethod]
	public async Task When_LoginAndRefresh_Then_TokenValuesAbsentFromLogs()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(default, cancellationToken: cts.Token);
		await harness.Authentication.RefreshAsync(cts.Token);

		harness.Logs.Text.Should().NotBeEmpty("Trace logging is on, so the providers must have said something");
		harness.Server.IssuedAccessTokens.Should().HaveCountGreaterThan(1, "sign-in and refresh each mint a token");
		foreach (var token in harness.Server.IssuedAccessTokens)
		{
			harness.Logs.Text.Should().NotContain(token, "tokens must never reach the log output");
		}
	}

	/// <summary>
	/// Red test for spec 017 F3: <see cref="WebAuthenticatorBrowser"/> used to swallow
	/// <see cref="OperationCanceledException"/> into <c>BrowserResultType.UnknownError</c>, so a
	/// caller-cancelled sign-in surfaced as "login failed" instead of propagating cancellation.
	/// </summary>
	[TestMethod]
	public async Task When_BrowserInvokeCancelled_Then_CancellationPropagates()
	{
		var browser = new WebAuthenticatorBrowser();
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var options = new BrowserOptions("https://stub-idp.example/connect/authorize", "oidc-tests://callback");
		Func<Task> act = () => browser.InvokeAsync(options, cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
