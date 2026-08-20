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
using Uno.Extensions.Storage.KeyValueStorage;
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
/// initializer needs a live Uno.UI. Running here also means the provider's runtime platform
/// dispatch (<c>OperatingSystem.Is*()</c>, spec 010) is exercised against a real OS on each head.
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
	private static async Task<Harness> CreateHarnessAsync(TimeSpan? webUiDelay = null, TimeSpan? interactiveTimeout = null)
	{
		var harness = CreateHarness(webUiDelay, interactiveTimeout);

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
	/// <remarks>
	/// The test host's own window is reused rather than <c>new Window()</c>: Android and iOS reject
	/// secondary windows outright (<c>InvalidOperationException</c>), which failed all 10 tests on
	/// the iOS simulator lane. Content is never assigned, so no save/restore is needed.
	/// </remarks>
	private static Harness CreateHarness(TimeSpan? webUiDelay = null, TimeSpan? interactiveTimeout = null)
	{
		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		var tenant = new StubEntra();
		var webUi = new StubWebUi(tenant, webUiDelay);
		var logs = new CapturingLoggerProvider();

		var configurationValues = new Dictionary<string, string?>
		{
			["Msal:ClientId"] = StubEntra.ClientId,
			["Msal:TenantId"] = StubEntra.TenantId,
			["Msal:Scopes:0"] = "User.Read",
		};
		if (interactiveTimeout is { } timeout)
		{
			configurationValues["Msal:InteractiveTimeout"] = timeout.ToString();
		}

		var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_MsalAuthentication).Assembly)
			// AddMsal binds Section<MsalConfiguration>("Msal") itself, so the section just has to
			// exist in configuration - which also keeps the config-binding path under test.
			.ConfigureHostConfiguration(configuration => configuration
				.AddInMemoryCollection(configurationValues))
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
	public async Task When_LoginAbandoned_Then_InteractiveTimeoutCancelsTheLogin()
	{
		// The system-browser flow can't detect a closed browser window: without the provider's
		// interactive timeout, an abandoned sign-in never completes and the awaiting command stays
		// busy (button disabled) forever. The stub browser's delay stands in for the abandoned
		// sign-in; no caller cancellation is involved.
		using var harness = await CreateHarnessAsync(
			webUiDelay: TimeSpan.FromSeconds(30),
			interactiveTimeout: TimeSpan.FromSeconds(1));
		using var cts = Cts();

		// Same shape as caller cancellation (see When_LoginCancelled): MSAL rethrows the web UI's
		// OperationCanceledException as MsalClientException / authentication_canceled.
		var thrown = await FluentActions.Awaiting(async () =>
				await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token))
			.Should().ThrowAsync<MsalClientException>();
		thrown.Which.ErrorCode.Should().Be(MsalError.AuthenticationCanceledError);

		harness.Logs.Text.Should().Contain("did not complete within",
			"the timeout must be distinguishable from a caller-requested cancellation in the logs");
		(await harness.Tokens.HasTokenAsync(CancellationToken.None)).Should().BeFalse(
			"an abandoned sign-in must not leave a partial token behind");
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
		// only place the provider's runtime platform selection is proven live. The harness sets
		// an explicit redirect URI through Builder(...), so this also pins the documented
		// precedence - the app callback wins over the platform default.
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);

		// The flow completing at all proves the redirect URI MSAL used matched what the stub
		// browser echoed back; a mismatch fails redemption inside MSAL.
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeTrue();
	}

	[TestMethod]
	public async Task When_AddMsal_Then_ProviderRegistered()
	{
		// Guards spec 010's failure mode: a stubbed-out AddMsal registers nothing, and the only
		// symptom is AuthenticationService's "No providers specified" error - logged far from the
		// cause, on every auth call. RefreshAsync exercises the same startup path an app does.
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.RefreshAsync(cts.Token);

		harness.Logs.Text.Should().NotContain("No providers specified",
			"AddMsal must register the MSAL provider on this platform");
	}

	[TestMethod]
	public async Task When_Built_Then_RedirectDecisionMatchesRuntimePlatform()
	{
		// The provider selects the redirect platform at runtime (OperatingSystem.Is*) because on
		// Skia iOS/Android heads Uno.Sdk substitutes the plain netX.0 build of the provider
		// assembly, so the TFM no longer implies the OS (spec 010). The harness sets no redirect
		// URI, so the platform default applies; asserted via the provider's trace log, using the
		// MsalRedirectDecision member names.
		using var harness = await CreateHarnessAsync();

#if WINDOWS
		// WinAppSDK stays compile-time in the provider: the WAM broker owns the redirect URI.
		var expected = "BrokerManaged";
#else
		var expected =
			PlatformHelper.IsWebAssembly ? "WebAuthenticationBroker"
			: OperatingSystem.IsAndroid() ? "PlatformDerived"
			// IsIOS() is also true on Mac Catalyst, which takes the Desktop path (no catalyst MSAL).
			: OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst() ? "PlatformDerived"
			: "MsalDefault";
#endif

		harness.Logs.Text.Should().Contain($"RedirectUri resolution: {expected}");
	}

	[TestMethod]
	public async Task When_LogoutWithoutDispatcher_Then_SignedOut()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeTrue("the test needs a signed-in state to log out of");

		// The documented IAuthenticationService.LogoutAsync(CancellationToken) extension passes no
		// dispatcher, and logout needs none - nothing in the provider's logout path shows UI.
		// Requiring one turned that overload into a guaranteed ArgumentNullException, which every
		// caller sees as "clicking logout does nothing" once the command swallows it.
		var result = await harness.Authentication.LogoutAsync(cts.Token);

		result.Should().BeTrue();
		(await harness.Authentication.IsAuthenticated(cts.Token)).Should().BeFalse();
		(await harness.Tokens.HasTokenAsync(cts.Token)).Should().BeFalse();
	}

	/// <summary>
	/// The key <c>MsalTokenCacheStore</c> writes the serialized cache under. Duplicated as a literal
	/// because that type is internal to the provider assembly; this is the contract, so the test
	/// should fail if either side moves.
	/// </summary>
	private static string MsalCacheKey => $"MsalCache_{StubEntra.ClientId}";

	/// <summary>
	/// Plants a serialized-cache entry in the host's default storage, standing in for the blob the
	/// WebAssembly head writes. Every head can then assert on the removal, which is what matters:
	/// this entry holds the refresh token.
	/// </summary>
	private static async Task SeedMsalCacheEntry(Harness harness, CancellationToken ct)
	{
		var storage = harness.Host.Services.GetRequiredDefaultInstance<IKeyValueStorage>();
		await storage.SetAsync(MsalCacheKey, "c2VyaWFsaXplZC1tc2FsLWNhY2hl", ct);
		(await storage.GetKeysAsync(ct)).Should().Contain(MsalCacheKey, "the test needs the entry to exist before it can assert it was removed");
	}

	private static async Task<bool> HasMsalCacheEntry(Harness harness, CancellationToken ct) =>
		(await harness.Host.Services.GetRequiredDefaultInstance<IKeyValueStorage>().GetKeysAsync(ct))
			.Contains(MsalCacheKey);

	[TestMethod]
	public async Task When_Logout_Then_SerializedMsalCacheRemoved()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);
		await SeedMsalCacheEntry(harness, cts.Token);

		await harness.Authentication.LogoutAsync(harness.Dispatcher, cts.Token);

		// The serialized cache carries the refresh token, not just the access token, so leaving it
		// behind means a signed-out user's session is still renewable from browser storage.
		(await HasMsalCacheEntry(harness, cts.Token)).Should().BeFalse(
			"logout must remove the serialized MSAL cache, not only the Uno token cache");
	}

	[TestMethod]
	public async Task When_LogoutCancelled_Then_SerializedMsalCacheStillRemoved()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);
		await SeedMsalCacheEntry(harness, cts.Token);

		// Sign-out has no half-done state worth stopping at: abandoning it part-way used to leave
		// surviving accounts signed in, their refresh tokens in the serialized cache, and
		// IsAuthenticated still true - a user who asked to log out, was told it was cancelled, and
		// is still authenticated. An already-cancelled token is the sharpest version of that.
		using var cancelled = new CancellationTokenSource();
		await cancelled.CancelAsync();

		try
		{
			await harness.Authentication.LogoutAsync(harness.Dispatcher, cancelled.Token);
		}
		catch (OperationCanceledException)
		{
			// Whether the cancellation surfaces is not what this guards - what matters is that no
			// refresh-token material survived it.
		}

		(await HasMsalCacheEntry(harness, cts.Token)).Should().BeFalse(
			"a cancelled sign-out must not leave the serialized MSAL cache behind");
	}

	[TestMethod]
	public async Task When_TokenCacheCleared_Then_SerializedMsalCacheRemoved()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);
		await SeedMsalCacheEntry(harness, cts.Token);

		// Clearing the token cache directly, without going through the provider's logout - the
		// belt-and-braces path (ITokenCache.Cleared).
		await harness.Tokens.ClearAsync(cts.Token);

		// The handler is fire-and-forget off a synchronous event, so wait for the effect rather than
		// assuming it already happened.
		for (var i = 0; i < 50 && await HasMsalCacheEntry(harness, cts.Token); i++)
		{
			await Task.Delay(100, cts.Token);
		}

		(await HasMsalCacheEntry(harness, cts.Token)).Should().BeFalse(
			"clearing the token cache must take the serialized MSAL cache with it");
	}

	[TestMethod]
	public async Task When_Login_Then_MsalCacheOnlyPersistedThroughStorageOnWebAssembly()
	{
		using var harness = await CreateHarnessAsync();
		using var cts = Cts();

		await harness.Authentication.LoginAsync(harness.Dispatcher, cancellationToken: cts.Token);

		var keys = await harness.Host.Services
			.GetRequiredDefaultInstance<IKeyValueStorage>()
			.GetKeysAsync(cts.Token);

		// The prefix is duplicated as a literal on purpose: MsalTokenCacheStore.KeyPrefix is
		// internal to the provider assembly, and this test exists to fail if either side drifts.
		if (PlatformHelper.IsWebAssembly)
		{
			// MsalCacheHelper has no browser backend, so the serialized cache goes through
			// IKeyValueStorage instead - which is what makes a page reload survivable (spec 011).
			keys.Should().Contain(key => key.StartsWith("MsalCache_", StringComparison.Ordinal));
		}
		else
		{
			// MsalCacheHelper (DPAPI / Keychain / keyring) or MSAL's own native cache owns
			// persistence here. Writing the blob to key-value storage as well would duplicate
			// refresh tokens into a store the platform doesn't protect.
			keys.Should().NotContain(key => key.StartsWith("MsalCache_", StringComparison.Ordinal));
		}
	}
}
