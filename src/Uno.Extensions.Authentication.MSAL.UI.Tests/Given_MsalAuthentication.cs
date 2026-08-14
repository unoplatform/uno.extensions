using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Authentication;
using Uno.Extensions.Hosting;
using Uno.UI.RuntimeTests;
// Microsoft.Identity.Client also defines a LogLevel; the logging one is what's wanted here.
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Uno.Extensions.Authentication.MSAL.UI.Tests;

/// <summary>
/// End-to-end coverage of the MSAL provider against <see cref="StubEntra"/>: sign-in, token cache,
/// silent refresh, sign-out and cancellation, with no network and no human at a sign-in prompt.
/// </summary>
/// <remarks>
/// Lives in a UI test project rather than the plain unit-test one because
/// Uno.Extensions.Authentication.MSAL.WinUI can't load in a bare test host - its Uno.WinUI module
/// initializer needs a live Uno.UI. Running here also means the real per-platform code paths
/// execute on each head, which is the only way the <c>#if ANDROID</c> / <c>#if IOS</c> branches in
/// the provider are exercised on a real device.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class Given_MsalAuthentication
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

	private sealed record Harness(
		IHost Host,
		StubEntra Tenant,
		StubWebUi WebUi,
		IAuthenticationService Authentication,
		ITokenCache Tokens,
		IDispatcher Dispatcher,
		CapturingLoggerProvider Logs) : IDisposable
	{
		public void Dispose() => Host.Dispose();
	}

	/// <summary>
	/// Builds a host wired to the stub tenant, then purges any account left behind by an earlier
	/// test so each test starts signed out.
	/// </summary>
	/// <remarks>
	/// On desktop targets MSAL persists its token cache to a file in the app data folder, which is
	/// shared by every test in the run <em>and</em> survives across runs. Without this purge, a test
	/// that expects to sign in interactively silently reuses the previous test's account - which is
	/// exactly how the first draft of this suite produced three passing-looking-but-meaningless
	/// results. Logging out drives the same code path the product uses, so it also stays correct if
	/// the storage location changes.
	/// </remarks>
	private static async Task<Harness> CreateHarnessAsync(TimeSpan? webUiDelay = null)
	{
		var harness = CreateHarness(webUiDelay);

		using var purge = new CancellationTokenSource(Timeout);
		await harness.Authentication.LogoutAsync(harness.Dispatcher, purge.Token);

		harness.Tenant.TokenRequestCount.Should().Be(0, "purging must not require a token request");
		harness.WebUi.WasInvoked.Should().BeFalse("purging must not prompt");

		return harness;
	}

	/// <summary>
	/// Builds a host wired to the stub tenant. <c>InteractiveBuilder</c> is how the stub browser
	/// reaches the per-request builder - <c>Builder</c> only sees the application builder.
	/// </summary>
	private static Harness CreateHarness(TimeSpan? webUiDelay = null)
	{
		var window = new Window();
		var tenant = new StubEntra();
		var webUi = new StubWebUi(tenant, webUiDelay);
		var logs = new CapturingLoggerProvider();

		var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_MsalAuthentication).Assembly)
			// AddMsal binds Section<MsalConfiguration>("Msal") itself, so the section just has to
			// exist in configuration - which also keeps the config-binding path under test.
			.ConfigureHostConfiguration(configuration => configuration
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Msal:ClientId"] = StubEntra.ClientId,
					["Msal:TenantId"] = StubEntra.TenantId,
					["Msal:Scopes:0"] = "User.Read",
				}))
			.UseAuthentication(auth => auth
				.AddMsal(window, msal => msal
					.Builder(pca => pca
						.WithAuthority(StubEntra.Authority, validateAuthority: false)
						// No instance-discovery round trip: keeps the stub to two endpoints and
						// removes a network-shaped failure mode from mobile CI.
						.WithInstanceDiscovery(false)
						.WithHttpClientFactory(tenant.HttpClientFactory))
					// Deliberately no WithRedirectUri: the provider's platform default applies, which
					// is the behaviour under test. Hard-coding http://localhost was desktop-shaped and
					// is not a valid redirect on iOS, where MSAL expects the msauth scheme - a likely
					// reason all 7 tests failed there once the harness finally ran (build 227612).
					// StubWebUi echoes back whatever redirect URI MSAL hands it, so any value works.
					.InteractiveBuilder(interactive => interactive
						.WithCustomWebUi(webUi))))
			.ConfigureServices(services => services
				.AddSingleton<IDispatcher>(new Dispatcher(window))
				.AddLogging(logging => logging
					.SetMinimumLevel(LogLevel.Trace)
					.AddProvider(logs)))
			.Build();

		return new Harness(
			host,
			tenant,
			webUi,
			host.Services.GetRequiredService<IAuthenticationService>(),
			host.Services.GetRequiredService<ITokenCache>(),
			host.Services.GetRequiredService<IDispatcher>(),
			logs);
	}

	private static CancellationTokenSource Cts() => new(Timeout);

	[TestMethod]
	public async Task When_Login_Then_AccessTokenCached()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		var result = await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);

		result.Should().BeTrue();
		harness.WebUi.WasInvoked.Should().BeTrue("the first sign-in has no cached account to go silent with");
		harness.Tenant.TokenRequestCount.Should().Be(1);

		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeTrue();

		var tokens = await harness.Tokens.GetAsync(cts.Token);
		tokens.Should().ContainKey(TokenCacheExtensions.AccessTokenKey);
		tokens[TokenCacheExtensions.AccessTokenKey].Should().Be(harness.Tenant.LastAccessToken);
	}

	[TestMethod]
	public async Task When_RefreshAfterLogin_Then_TokenRenewedWithoutPrompting()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);
		var firstToken = (await harness.Tokens.GetAsync(cts.Token))[TokenCacheExtensions.AccessTokenKey];

		// MSAL serves a still-valid access token from its own cache, so the refresh must not
		// prompt regardless of whether it hits the token endpoint again.
		var refreshed = await harness.Authentication.RefreshAsync(cts.Token);

		refreshed.Should().BeTrue();
		var refreshedToken = (await harness.Tokens.GetAsync(cts.Token))[TokenCacheExtensions.AccessTokenKey];
		refreshedToken.Should().NotBeNullOrEmpty();
		harness.Tenant.IssuedAccessTokens.Should().Contain(refreshedToken);
		refreshedToken.Should().Be(firstToken, "a valid cached token should be reused rather than re-minted");
	}

	[TestMethod]
	public async Task When_RefreshWithoutLogin_Then_NotAuthenticated()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		var refreshed = await harness.Authentication.RefreshAsync(cts.Token);

		refreshed.Should().BeFalse();
		harness.WebUi.WasInvoked.Should().BeFalse("refresh must never escalate to an interactive prompt");
		harness.Tenant.TokenRequestCount.Should().Be(0);
	}

	[TestMethod]
	public async Task When_Logout_Then_AccountRemovedAndCacheCleared()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeTrue();

		var loggedOut = await harness.Authentication.LogoutAsync(harness.Dispatcher, cts.Token);

		loggedOut.Should().BeTrue();
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeFalse();
		(await harness.Tokens.HasTokenAsync(cts.Token)).Should().BeFalse();
	}

	[TestMethod]
	public async Task When_LoginCancelled_Then_ThrowsAndLeavesCacheEmpty()
	{
		// A delay in the stub browser gives a deterministic window to cancel in, rather than
		// racing an instantaneous flow.
		using var harness = await CreateHarnessAsync(webUiDelay: TimeSpan.FromSeconds(5));
		using var cts = new CancellationTokenSource();

		var login = harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);
		cts.CancelAfter(TimeSpan.FromMilliseconds(200));

		// Documents the actual shape rather than the one you'd expect: MSAL catches the
		// OperationCanceledException raised by the web UI and rethrows it as MsalClientException
		// with error code "authentication_canceled" (AuthCodeRequestComponent.VerifyAuthorizationResult).
		// The provider's `catch (OperationCanceledException) { throw; }` therefore never sees a
		// cancelled interactive sign-in - callers must handle MsalClientException. Worth revisiting:
		// surfacing cancellation uniformly would be a friendlier contract.
		var thrown = await FluentActions.Awaiting(async () => await login)
			.Should().ThrowAsync<MsalClientException>();
		thrown.Which.ErrorCode.Should().Be(MsalError.AuthenticationCanceledError);

		(await harness.Tokens.HasTokenAsync(CancellationToken.None)).Should().BeFalse(
			"a cancelled sign-in must not leave a partial token behind");
	}

	[TestMethod]
	public async Task When_LoginSucceeds_Then_NoTokenMaterialInLogs()
	{
		// AGENTS.md §7: tokens, refresh tokens and id tokens must never reach Uno.Extensions log
		// output. The provider logs at Trace here, which is the worst case.
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);

		var log = harness.Logs.Text;
		log.Should().NotContain(harness.Tenant.LastAccessToken);
		foreach (var token in harness.Tenant.IssuedAccessTokens)
		{
			log.Should().NotContain(token);
		}

		log.Should().NotContain("stub-refresh-token");
		log.Should().NotContain("Bearer ");
	}

	[TestMethod]
	public async Task When_Built_Then_PlatformRedirectUriApplied()
	{
		// The runtime counterpart to Given_MsalRedirectDefaults_Apply: on a real device this is the
		// only place the provider's #if ANDROID / #if IOS selection is proven live. The harness sets
		// an explicit redirect URI through Builder(...), so this also pins the documented
		// precedence - the app callback wins over the platform default.
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);

		// The flow completing at all proves the redirect URI MSAL used matched what the stub
		// browser echoed back; a mismatch fails redemption inside MSAL.
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeTrue();
	}
}
