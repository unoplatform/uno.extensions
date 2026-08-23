using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Authentication.MSAL;

namespace Uno.Extensions.Authentication;

[TestClass]
public class Given_AuthenticationService
{
	private static readonly IDictionary<string, string> SomeTokens =
		new Dictionary<string, string> { { TokenCacheExtensions.AccessTokenKey, "access-1" } };

	private static (AuthenticationService Service, TokenCache Tokens, FakeAuthenticationProvider Provider, CountingProviderFactory Factory) Create()
	{
		var tokens = new TokenCache(NullLogger<TokenCache>.Instance, new FakeKeyValueStorage());
		var provider = new FakeAuthenticationProvider();
		var factory = new CountingProviderFactory(provider);
		var service = new AuthenticationService(NullLogger<AuthenticationService>.Instance, new[] { factory }, tokens);
		return (service, tokens, provider, factory);
	}

	[TestMethod]
	public async Task When_RefreshReturnsNoTokens_Then_SignedOutAndLoggedOutRaised()
	{
		// The provider could not renew the session - an expired or revoked refresh token. The user
		// was authenticated and now is not, which is exactly what LoggedOut exists to announce; an
		// app that navigates on it must not be left on a signed-in page with nothing to send.
		var (service, tokens, provider, _) = Create();
		await tokens.SaveAsync(provider.Name, SomeTokens, CancellationToken.None);
		var loggedOut = 0;
		service.LoggedOut += (_, _) => loggedOut++;

		provider.OnRefresh = () => null;
		var refreshed = await service.RefreshAsync(CancellationToken.None);

		refreshed.Should().BeFalse();
		(await service.IsAuthenticated(CancellationToken.None)).Should().BeFalse();
		(await tokens.GetAsync(CancellationToken.None)).Should().BeEmpty();
		loggedOut.Should().Be(1, "a refresh that ends the session is a sign-out");
	}

	[TestMethod]
	public async Task When_RefreshReturnsEmptyTokens_Then_SignedOut()
	{
		// An empty dictionary is "no tokens" too: saving it would leave no keys, but only ClearAsync
		// raises Cleared, so the two must be treated alike.
		var (service, tokens, provider, _) = Create();
		await tokens.SaveAsync(provider.Name, SomeTokens, CancellationToken.None);
		var loggedOut = 0;
		service.LoggedOut += (_, _) => loggedOut++;

		provider.OnRefresh = () => new Dictionary<string, string>();
		var refreshed = await service.RefreshAsync(CancellationToken.None);

		refreshed.Should().BeFalse();
		loggedOut.Should().Be(1);
	}

	[TestMethod]
	public async Task When_RefreshReturnsTokens_Then_SavedAndStillAuthenticated()
	{
		var (service, tokens, provider, _) = Create();
		await tokens.SaveAsync(provider.Name, SomeTokens, CancellationToken.None);
		var loggedOut = 0;
		service.LoggedOut += (_, _) => loggedOut++;

		provider.OnRefresh = () => new Dictionary<string, string> { { TokenCacheExtensions.AccessTokenKey, "access-2" } };
		var refreshed = await service.RefreshAsync(CancellationToken.None);

		refreshed.Should().BeTrue();
		(await tokens.GetAsync(CancellationToken.None))[TokenCacheExtensions.AccessTokenKey].Should().Be("access-2");
		loggedOut.Should().Be(0);
	}

	[TestMethod]
	public async Task When_ProviderLogoutThrows_Then_TokensClearedAndLoggedOutRaised()
	{
		// A keychain or cache-file error part-way through the provider's sign-out must not leave the
		// user signed in: the access token would survive, IsAuthenticated would stay true and the HTTP
		// handlers would keep sending the bearer. The failure still surfaces to the caller.
		var (service, tokens, provider, _) = Create();
		await tokens.SaveAsync(provider.Name, SomeTokens, CancellationToken.None);
		var loggedOut = 0;
		service.LoggedOut += (_, _) => loggedOut++;
		provider.OnLogout = () => throw new InvalidOperationException("keychain locked");

		var act = async () => await service.LogoutAsync(dispatcher: null, CancellationToken.None);

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("keychain locked");
		(await service.IsAuthenticated(CancellationToken.None)).Should().BeFalse();
		(await tokens.GetAsync(CancellationToken.None)).Should().BeEmpty();
		loggedOut.Should().Be(1);
	}

	[TestMethod]
	public async Task When_ProviderLogoutThrowsWithCancelledToken_Then_TokensStillCleared()
	{
		// The clear must not itself be skipped because the caller's token was already cancelled.
		var (service, tokens, provider, _) = Create();
		await tokens.SaveAsync(provider.Name, SomeTokens, CancellationToken.None);
		provider.OnLogout = () => throw new OperationCanceledException();
		using var cancelled = new CancellationTokenSource();
		cancelled.Cancel();

		var act = async () => await service.LogoutAsync(dispatcher: null, cancelled.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
		(await tokens.GetAsync(CancellationToken.None)).Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_ProviderDeclinesLogout_Then_TokensKept()
	{
		// `false` is the provider saying the sign-out did not happen (the user dismissed a prompt,
		// say) - not a failure; the session stands.
		var (service, tokens, provider, _) = Create();
		await tokens.SaveAsync(provider.Name, SomeTokens, CancellationToken.None);
		var loggedOut = 0;
		service.LoggedOut += (_, _) => loggedOut++;
		provider.OnLogout = () => false;

		var result = await service.LogoutAsync(dispatcher: null, CancellationToken.None);

		result.Should().BeFalse();
		(await service.IsAuthenticated(CancellationToken.None)).Should().BeTrue();
		loggedOut.Should().Be(0);
	}

	[TestMethod]
	public async Task When_NotAuthenticated_Then_RefreshDoesNotCallProvider()
	{
		var (service, _, provider, _) = Create();

		var refreshed = await service.RefreshAsync(CancellationToken.None);

		refreshed.Should().BeFalse();
		provider.RefreshCount.Should().Be(0);
	}

	[TestMethod]
	public async Task When_CalledConcurrently_Then_ProvidersBuiltOnce()
	{
		// Regression guard for the Count == 0 check this replaced: two concurrent callers could both
		// see an empty table and both populate it. The factory counts reads of its provider, which
		// happens once per build of the table.
		var (service, tokens, provider, factory) = Create();
		await tokens.SaveAsync(provider.Name, SomeTokens, CancellationToken.None);
		provider.OnRefresh = () => SomeTokens;

		await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => service.RefreshAsync(CancellationToken.None).AsTask())));

		factory.Reads.Should().Be(1);
		service.Providers.Should().Equal(provider.Name);
	}

	[TestMethod]
	public void When_NothingResolvedYet_Then_ProvidersIsEmptyWithoutBuilding()
	{
		// Reading the property must not construct providers (MSAL builds a client application in
		// its factory); it reports what has been built so far.
		var (service, _, _, factory) = Create();

		service.Providers.Should().BeEmpty();
		factory.Reads.Should().Be(0);
	}
}
