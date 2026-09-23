using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Authentication;

[TestClass]
public class Given_TokenCache
{
	private const string AccessToken = "access-token";
	private const int MaxValueLength = 64;
	private static readonly string OversizedIdToken = new('x', MaxValueLength + 1);

	[TestMethod]
	public async Task When_IdTokenRejectedByStorage_Then_SessionKeptWithoutIt()
	{
		// Packaged WinAppSDK LocalSettings caps a value at 8 KB, which a claim-heavy ID token (B2C
		// custom attributes, an Entra groups claim) can exceed. The ID token is optional, so that
		// must not fail a sign-in that has a perfectly good access token.
		var log = new CapturingLogger<TokenCache>();
		var cache = new TokenCache(log, new FakeKeyValueStorage { MaxValueLength = MaxValueLength });

		// ID token first: the save must not depend on the dictionary's order to land the access token.
		await cache.SaveAsync("Fake", new Dictionary<string, string>
		{
			{ TokenCacheExtensions.IdTokenKey, OversizedIdToken },
			{ TokenCacheExtensions.AccessTokenKey, AccessToken },
		}, CancellationToken.None);

		var tokens = await cache.GetAsync(CancellationToken.None);
		tokens.Should().ContainKey(TokenCacheExtensions.AccessTokenKey).WhoseValue.Should().Be(AccessToken);
		tokens.Should().NotContainKey(TokenCacheExtensions.IdTokenKey);
		log.Text.Should().Contain("Warning").And.Contain(TokenCacheExtensions.IdTokenKey);
		log.Text.Should().NotContain(OversizedIdToken);
	}

	[TestMethod]
	public async Task When_AccessTokenRejectedByStorage_Then_SaveThrows()
	{
		// Only the ID token is best-effort: a session without its access token is not a session.
		var cache = new TokenCache(new CapturingLogger<TokenCache>(), new FakeKeyValueStorage { MaxValueLength = MaxValueLength });

		var save = async () => await cache.SaveAsync("Fake", new Dictionary<string, string>
		{
			{ TokenCacheExtensions.AccessTokenKey, new string('x', MaxValueLength + 1) },
		}, CancellationToken.None);

		await save.Should().ThrowAsync<InvalidOperationException>();
	}
}
