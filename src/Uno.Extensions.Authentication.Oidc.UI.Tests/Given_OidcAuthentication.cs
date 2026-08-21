using System;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.OidcClient.Browser;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Authentication;
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
		ITokenCache Tokens) : IDisposable
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
			.ConfigureServices(services => services.AddSingleton<IBrowser>(browser))
			.Build();

		return new Harness(
			host,
			server,
			browser,
			host.Services.GetRequiredService<IAuthenticationService>(),
			host.Services.GetRequiredService<ITokenCache>());
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
	/// Red test for spec 012 F1: the provider used to ignore <c>RefreshTokenResult.IsError</c> and
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
	/// Red test for spec 012 F2: the provider used to ignore <c>LogoutResult.IsError</c> and return
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
	/// Red test for spec 012 F3: <see cref="WebAuthenticatorBrowser"/> used to swallow
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
