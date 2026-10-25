#if !__ANDROID__ && !__IOS__ && !__WASM__ && !WINDOWS && !__MACCATALYST__
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.Jwk;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Authentication.Oidc;
using Windows.Security.Authentication.Web;

namespace Uno.Extensions.Authentication.UI.Tests;

/// <summary>
/// The OIDC provider over the desktop loopback broker's callback: with <c>AutoRedirectUri</c> the
/// redirect must follow the broker's current callback on every flow, not the one captured when
/// the provider was built - the broker moves to another port when its default one could not be
/// bound. Sign-out must carry the cached id_token as its hint, or the identity provider never
/// redirects back and the broker waits out its timeout.
/// </summary>
[TestClass]
[DoNotParallelize]
public class Given_OidcAuthenticationProvider
{
	[TestMethod]
	public async Task When_AutoRedirectUri_Then_RedirectFollowsBrokerCallbackOnEveryFlow()
	{
		StaticBrokerRegistration.EnsureRegistered();
		var browser = new RecordingBrowser();
		var provider = CreateProvider(browser, new InMemoryTokenCache());
		var firstCallback = new Uri("http://localhost:5101/authentication-callback");
		var secondCallback = new Uri("http://localhost:5102/authentication-callback");
		var original = WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		try
		{
			// The pinned return URI stands in for the broker's default port moving between flows.
			WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri = firstCallback;
			provider.Build();

			await provider.LoginAsync(dispatcher: null, credentials: null, cts.Token);
			WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri = secondCallback;
			await provider.LoginAsync(dispatcher: null, credentials: null, cts.Token);

			browser.EndUrls.Should().Equal(
				[firstCallback.OriginalString, secondCallback.OriginalString],
				"each flow must send the identity provider to the broker's current callback");
		}
		finally
		{
			WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri = original;
		}
	}

	[TestMethod]
	public async Task When_Logout_Then_CachedIdTokenSentAsHint()
	{
		StaticBrokerRegistration.EnsureRegistered();
		var browser = new RecordingBrowser();
		var tokens = new InMemoryTokenCache();
		await tokens.SaveAsync(
			OidcAuthenticationProvider.DefaultName,
			new Dictionary<string, string>
			{
				[TokenCacheExtensions.AccessTokenKey] = "stub-access-token",
				[TokenCacheExtensions.IdTokenKey] = "stub-id-token",
			},
			CancellationToken.None);
		var provider = CreateProvider(browser, tokens);
		provider.Build();
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		await provider.LogoutAsync(dispatcher: null, cts.Token);

		browser.StartUrls.Should().ContainSingle()
			.Which.Should().StartWith(EndSessionEndpoint)
			.And.Contain("id_token_hint=stub-id-token", "without the hint the identity provider does not redirect back after sign-out");
	}

	private const string EndSessionEndpoint = "https://stub-idp.example/endsession";

	private static OidcAuthenticationProvider CreateProvider(IBrowser browser, ITokenCache tokens)
		=> new(
			NullLogger<OidcAuthenticationProvider>.Instance,
			browser,
			new NoConfiguration(),
			tokens,
			new OidcAuthenticationSettings
			{
				AutoRedirectUri = true,
				Options = new OidcClientOptions
				{
					ClientId = "client",
					// No discovery round trip: the endpoints are given, and nothing is validated
					// because the fake browser never returns a response.
					ProviderInformation = new ProviderInformation
					{
						IssuerName = "https://stub-idp.example",
						AuthorizeEndpoint = "https://stub-idp.example/authorize",
						TokenEndpoint = "https://stub-idp.example/token",
						EndSessionEndpoint = EndSessionEndpoint,
						KeySet = new JsonWebKeySet(),
					},
					LoadProfile = false,
				},
			});

	/// <summary>Records the start and redirect URI of each browser flow, and cancels the flow.</summary>
	private sealed class RecordingBrowser : IBrowser
	{
		public List<string> StartUrls { get; } = new();

		public List<string> EndUrls { get; } = new();

		public Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
		{
			StartUrls.Add(options.StartUrl);
			EndUrls.Add(options.EndUrl);
			return Task.FromResult(new BrowserResult { ResultType = BrowserResultType.UserCancel });
		}
	}

	/// <summary>No configured section: the settings' Options are what the provider builds from.</summary>
	private sealed class NoConfiguration : IOptionsSnapshot<OidcClientOptions>
	{
		public OidcClientOptions Value => new();

		public OidcClientOptions Get(string? name) => new();
	}

	private sealed class InMemoryTokenCache : ITokenCache
	{
		private IDictionary<string, string> _tokens = new Dictionary<string, string>();
		private string? _provider;

		public event EventHandler? Cleared;

		public ValueTask<string?> GetCurrentProviderAsync(CancellationToken ct) => new(_provider);

		public ValueTask<bool> HasTokenAsync(CancellationToken cancellationToken) => new(_tokens.Count > 0);

		public ValueTask<IDictionary<string, string>> GetAsync(CancellationToken cancellationToken) => new(_tokens);

		public ValueTask SaveAsync(string provider, IDictionary<string, string>? tokens, CancellationToken cancellationToken)
		{
			_provider = provider;
			_tokens = tokens ?? new Dictionary<string, string>();
			return default;
		}

		public ValueTask ClearAsync(CancellationToken cancellationToken)
		{
			_tokens = new Dictionary<string, string>();
			_provider = null;
			Cleared?.Invoke(this, EventArgs.Empty);
			return default;
		}
	}
}
#endif
