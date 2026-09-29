using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Authentication.Handlers;

namespace Uno.Extensions.Authentication;

/// <summary>
/// AGENTS.md section 7: tokens must never reach Uno.Extensions log output. Both cases here log at
/// the level the code actually uses - Trace for the cache, Debug for the handler - on a logger
/// that records everything, which is what a consumer's pipeline sees once those levels are on.
/// </summary>
[TestClass]
public class Given_TokenLogging
{
	private const string AccessToken = "eyJ-access-token-value-that-must-not-be-logged";
	private const string RefreshToken = "refresh-token-value-that-must-not-be-logged";

	private static async Task<(TokenCache Cache, CapturingLogger<TokenCache> Log)> SeededCache()
	{
		var log = new CapturingLogger<TokenCache>();
		var cache = new TokenCache(log, new FakeKeyValueStorage());
		await cache.SaveAsync("Fake", new Dictionary<string, string>
		{
			{ TokenCacheExtensions.AccessTokenKey, AccessToken },
			{ TokenCacheExtensions.RefreshTokenKey, RefreshToken },
		}, CancellationToken.None);
		return (cache, log);
	}

	[TestMethod]
	public async Task When_KeysLoggedAtTrace_Then_TokenValuesAbsent()
	{
		// HasTokenAsync enumerates the cache at Trace, which used to print every value.
		var (cache, log) = await SeededCache();

		(await cache.HasTokenAsync(CancellationToken.None)).Should().BeTrue("the diagnostic only runs when there are keys to list");

		log.Text.Should().Contain(TokenCacheExtensions.AccessTokenKey, "the key is the useful part of the diagnostic");
		log.Text.Should().NotContain(AccessToken);
		log.Text.Should().NotContain(RefreshToken);
	}

	[TestMethod]
	public async Task When_HeaderApplied_Then_TokenNotLogged()
	{
		// HeaderHandler runs on every outbound request and used to log the bearer at Debug.
		var (cache, _) = await SeededCache();
		var log = new CapturingLogger<HeaderHandler>();
		var handler = new HeaderHandler(log, new FakeAuthenticationService(), cache, new HandlerSettings())
		{
			InnerHandler = new OkHandler(),
		};
		using var invoker = new HttpMessageInvoker(handler);
		using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example/resource");

		using var response = await invoker.SendAsync(request, CancellationToken.None);

		request.Headers.Authorization!.Parameter.Should().Be(AccessToken, "the header itself must still carry the token");
		log.Text.Should().Contain("Bearer", "the scheme is safe to log");
		log.Text.Should().NotContain(AccessToken);
	}

	private sealed class OkHandler : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request });
	}

	private sealed class FakeAuthenticationService : IAuthenticationService
	{
		public event EventHandler? LoggedOut { add { } remove { } }
		public string[] Providers => new[] { "Fake" };
		public ValueTask<bool> LoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials = null, string? provider = null, CancellationToken? cancellationToken = null) => new(true);
		public ValueTask<bool> RefreshAsync(CancellationToken? cancellationToken = null) => new(true);
		public ValueTask<bool> LogoutAsync(IDispatcher? dispatcher, CancellationToken? cancellationToken = null) => new(true);
		public ValueTask<bool> IsAuthenticated(CancellationToken? cancellationToken = null) => new(true);
	}
}
