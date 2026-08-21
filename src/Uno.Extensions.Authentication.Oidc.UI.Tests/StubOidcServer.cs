using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.OidcClient;

namespace Uno.Extensions.Authentication.Oidc.UI.Tests;

/// <summary>
/// A stand-in OpenID Connect provider: serves the token and end-session endpoints so the whole
/// Duende OidcClient pipeline - code redemption, refresh, sign-out - can run with no network and
/// no human at a sign-in prompt.
/// </summary>
/// <remarks>
/// <para>
/// Like <c>StubEntra</c> in the MSAL suite, this fakes the <em>transport</em> rather than
/// <see cref="OidcClient"/> itself, so state/PKCE handling and response validation stay in the
/// loop. Discovery is skipped by handing <see cref="ProviderInformation"/> to the options
/// directly, so only two endpoints matter.
/// </para>
/// <para>
/// Token responses carry no id_token: the code flow does not require one
/// (<c>ResponseProcessor.ValidateTokenResponseAsync</c> is called with
/// <c>requireIdentityToken: false</c>), and omitting it keeps this stub free of JWT minting.
/// </para>
/// </remarks>
internal sealed class StubOidcServer
{
	internal const string ClientId = "d3b7f1de-stub-oidc-client";
	internal const string Issuer = "https://stub-idp.example";

	/// <summary>Distinguishes the token minted for each request; see <see cref="LastAccessToken"/>.</summary>
	private int _tokenCounter;

	/// <summary>Every access token this stub has minted, oldest first.</summary>
	public List<string> IssuedAccessTokens { get; } = new();

	/// <summary>Number of POSTs to the token endpoint (any grant), so tests can assert a round-trip happened.</summary>
	public int TokenRequestCount { get; private set; }

	/// <summary>Number of refresh_token grants received.</summary>
	public int RefreshRequestCount { get; private set; }

	/// <summary>When set, the refresh_token grant fails with this OAuth error code (e.g. "invalid_grant").</summary>
	public string? RefreshError { get; set; }

	/// <summary>The most recently minted access token.</summary>
	public string LastAccessToken => IssuedAccessTokens.Count > 0
		? IssuedAccessTokens[IssuedAccessTokens.Count - 1]
		: throw new InvalidOperationException("No token has been issued yet.");

	/// <summary>Canned metadata handed to <see cref="OidcClientOptions.ProviderInformation"/>, skipping discovery.</summary>
	public ProviderInformation ProviderInformation { get; } = new()
	{
		IssuerName = Issuer,
		AuthorizeEndpoint = $"{Issuer}/connect/authorize",
		TokenEndpoint = $"{Issuer}/connect/token",
		EndSessionEndpoint = $"{Issuer}/connect/endsession",
		// Validate() insists on a key set even though no id_token is ever minted and signature
		// validation is opted out - empty satisfies it.
		KeySet = new Duende.IdentityModel.Jwk.JsonWebKeySet(),
	};

	/// <summary>
	/// Single client instance for <see cref="OidcClientOptions.HttpClientFactory"/>; the handler
	/// routes every request back into this stub.
	/// </summary>
	public HttpClient HttpClient { get; }

	public StubOidcServer() => HttpClient = new HttpClient(new StubHandler(this));

	private async Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var path = request.RequestUri?.AbsolutePath ?? string.Empty;

		if (path.EndsWith("/connect/token", StringComparison.Ordinal))
		{
			TokenRequestCount++;
			var form = request.Content is null
				? string.Empty
				: await request.Content.ReadAsStringAsync(cancellationToken);
			var grantType = GetFormValue(form, "grant_type");

			if (grantType == "refresh_token")
			{
				RefreshRequestCount++;
				if (RefreshError is { } error)
				{
					return Json(
						JsonSerializer.Serialize(new StubOidcErrorResponse { Error = error }, StubOidcJsonContext.Default.StubOidcErrorResponse),
						HttpStatusCode.BadRequest);
				}
			}

			return Json(TokenResponse());
		}

		if (path.EndsWith("/connect/endsession", StringComparison.Ordinal))
		{
			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		// Fail loudly: a silently-200'd unknown endpoint would surface as an unrelated OidcClient
		// error much later, which is painful to diagnose from a mobile CI log.
		return new HttpResponseMessage(HttpStatusCode.NotFound)
		{
			Content = new StringContent($"StubOidcServer has no handler for {request.Method} {request.RequestUri}"),
		};
	}

	private string TokenResponse()
	{
		var accessToken = $"stub-access-token-{++_tokenCounter}";
		IssuedAccessTokens.Add(accessToken);

		var response = new StubOidcTokenResponse
		{
			AccessToken = accessToken,
			RefreshToken = $"stub-refresh-token-{_tokenCounter}",
			ExpiresIn = 3600,
			Scope = "openid profile offline_access",
		};

		return JsonSerializer.Serialize(response, StubOidcJsonContext.Default.StubOidcTokenResponse);
	}

	private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
		new(status)
		{
			Content = new StringContent(body, Encoding.UTF8, "application/json"),
		};

	/// <summary>
	/// Minimal form-urlencoded reader. Avoids a dependency on WebUtility/HttpUtility parsing
	/// differences across the four heads this runs on.
	/// </summary>
	private static string? GetFormValue(string form, string key)
	{
		foreach (var pair in form.Split('&'))
		{
			var separatorIndex = pair.IndexOf('=');
			if (separatorIndex > 0 && pair.Substring(0, separatorIndex) == key)
			{
				return Uri.UnescapeDataString(pair.Substring(separatorIndex + 1));
			}
		}

		return null;
	}

	private sealed class StubHandler : HttpMessageHandler
	{
		private readonly StubOidcServer _server;

		public StubHandler(StubOidcServer server) => _server = server;

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var response = await _server.HandleAsync(request, cancellationToken);
			response.RequestMessage = request;
			return response;
		}
	}
}

/// <summary>Shape of the token endpoint response. Serialized through the source-generated context.</summary>
internal sealed record StubOidcTokenResponse
{
	[JsonPropertyName("token_type")]
	public string TokenType { get; init; } = "Bearer";

	[JsonPropertyName("access_token")]
	public string AccessToken { get; init; } = string.Empty;

	[JsonPropertyName("refresh_token")]
	public string RefreshToken { get; init; } = string.Empty;

	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; init; }

	[JsonPropertyName("scope")]
	public string Scope { get; init; } = string.Empty;
}

/// <summary>Shape of an OAuth error response from the token endpoint.</summary>
internal sealed record StubOidcErrorResponse
{
	[JsonPropertyName("error")]
	public string Error { get; init; } = string.Empty;
}

/// <summary>
/// Source-generated serialization (AGENTS.md §2) - also what keeps this working on the WebAssembly
/// and mobile heads, where reflection-based serialization is trimmed away.
/// </summary>
[JsonSerializable(typeof(StubOidcTokenResponse))]
[JsonSerializable(typeof(StubOidcErrorResponse))]
internal partial class StubOidcJsonContext : JsonSerializerContext
{
}
