using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Uno.Extensions.Authentication.MSAL.UI.Tests;

/// <summary>
/// A stand-in Microsoft Entra tenant: serves the OpenID configuration document and the token
/// endpoint so the whole MSAL pipeline - authorization-code redemption, silent refresh, cache
/// serialization - can run with no network, no tenant, and no human at a sign-in prompt.
/// </summary>
/// <remarks>
/// <para>
/// This intentionally fakes the <em>transport</em> rather than <see cref="IPublicClientApplication"/>.
/// MSAL's per-request builders (<c>AcquireTokenInteractiveParameterBuilder</c> and friends) are
/// concrete types that can't be substituted, and faking the client interface would skip exactly the
/// logic worth testing - PKCE, cache writes, account resolution. Stubbing HTTP keeps real MSAL in
/// the loop.
/// </para>
/// <para>
/// Instance discovery is disabled by the caller (<c>WithInstanceDiscovery(false)</c>), so only two
/// endpoints matter. Tokens are per-instance and monotonic, so a refreshed token is distinguishable
/// from the original.
/// </para>
/// </remarks>
internal sealed class StubEntra
{
	internal const string TenantId = "6babcaad-604b-40ac-a9d7-9fd97c0b779f";
	internal const string ClientId = "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b";
	internal const string ObjectId = "9f4880d8-80ba-4c40-97bc-f7a23c703084";
	internal const string Username = "test.user@contoso.example";
	internal const string Authority = $"https://login.microsoftonline.com/{TenantId}/";

	/// <summary>Distinguishes the token minted for each request; see <see cref="LastAccessToken"/>.</summary>
	private int _tokenCounter;

	/// <summary>
	/// Makes this instance's refresh tokens distinct from every other instance's. MSAL throttles
	/// silent requests process-wide for two minutes after an invalid_grant, keyed on client id,
	/// authority, scopes and the SHA-256 of the refresh token - so a counter alone let one test's
	/// rejected refresh token sign the next test out without a request ever being made.
	/// </summary>
	private readonly string _instanceId = Guid.NewGuid().ToString("N");

	/// <summary>Every access token this stub has minted, oldest first.</summary>
	public List<string> IssuedAccessTokens { get; } = new();

	/// <summary>Number of POSTs to the token endpoint, so tests can assert a network round-trip happened.</summary>
	public int TokenRequestCount { get; private set; }

	/// <summary>
	/// Lifetime of the access tokens this stub mints. MSAL treats a token that expires within its
	/// five-minute buffer as expired, so anything below that forces the next silent call through
	/// the refresh token - and therefore through the token endpoint.
	/// </summary>
	public int ExpiresInSeconds { get; set; } = 3599;

	/// <summary>
	/// When set, every token request gets this response instead of a freshly minted token.
	/// </summary>
	public Func<HttpResponseMessage>? TokenEndpointResponse { get; set; }

	/// <summary>
	/// Answers token requests the way Entra does once a refresh token has expired or been revoked:
	/// <c>400 invalid_grant</c>, which MSAL surfaces as <c>MsalUiRequiredException</c>.
	/// </summary>
	public void RejectRefreshTokens() => TokenEndpointResponse = () =>
		new HttpResponseMessage(HttpStatusCode.BadRequest)
		{
			Content = new StringContent(
				"""{"error":"invalid_grant","error_description":"AADSTS70008: The provided authorization code or refresh token has expired.","error_codes":[70008]}""",
				Encoding.UTF8,
				"application/json"),
		};

	/// <summary>
	/// Answers token requests with <c>503</c>, standing in for an unreachable or struggling token
	/// endpoint - a failure that says nothing about whether the session is still valid.
	/// </summary>
	public void FailTokenRequests() => TokenEndpointResponse = () =>
		new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
		{
			Content = new StringContent("""{"error":"temporarily_unavailable"}""", Encoding.UTF8, "application/json"),
		};

	/// <summary>The most recently minted access token.</summary>
	public string LastAccessToken => IssuedAccessTokens.Count > 0
		? IssuedAccessTokens[IssuedAccessTokens.Count - 1]
		: throw new InvalidOperationException("No token has been issued yet.");

	public IMsalHttpClientFactory HttpClientFactory => new StubHttpClientFactory(this);

	/// <summary>
	/// Returns the redirect URI MSAL should see coming back from the browser, echoing the request's
	/// <c>state</c> so MSAL's CSRF check passes.
	/// </summary>
	public Uri BuildAuthorizationResponse(Uri authorizationUri, Uri redirectUri)
	{
		var state = GetQueryValue(authorizationUri.Query, "state");
		var separator = string.IsNullOrEmpty(redirectUri.Query) ? "?" : "&";
		var response = $"{redirectUri}{separator}code=stub-auth-code-{++_tokenCounter}";
		if (state is { Length: > 0 })
		{
			response += $"&state={Uri.EscapeDataString(state)}";
		}

		return new Uri(response);
	}

	/// <summary>
	/// Minimal query reader. Avoids a dependency on WebUtility/HttpUtility parsing differences
	/// across the four heads this runs on.
	/// </summary>
	private static string? GetQueryValue(string query, string key)
	{
		foreach (var pair in query.TrimStart('?').Split('&'))
		{
			var separatorIndex = pair.IndexOf('=');
			if (separatorIndex > 0 && pair.Substring(0, separatorIndex) == key)
			{
				return Uri.UnescapeDataString(pair.Substring(separatorIndex + 1));
			}
		}

		return null;
	}

	private HttpResponseMessage Handle(HttpRequestMessage request)
	{
		var path = request.RequestUri?.AbsolutePath ?? string.Empty;

		if (path.EndsWith("/.well-known/openid-configuration", StringComparison.Ordinal))
		{
			return Json(OpenIdConfiguration());
		}

		if (path.EndsWith("/oauth2/v2.0/token", StringComparison.Ordinal))
		{
			TokenRequestCount++;
			return TokenEndpointResponse?.Invoke() ?? Json(TokenResponse());
		}

		// Fail loudly: a silently-200'd unknown endpoint would surface as an unrelated MSAL error
		// much later, which is painful to diagnose from a mobile CI log.
		return new HttpResponseMessage(HttpStatusCode.NotFound)
		{
			Content = new StringContent($"StubEntra has no handler for {request.Method} {request.RequestUri}"),
		};
	}

	private static HttpResponseMessage Json(string body) =>
		new(HttpStatusCode.OK)
		{
			Content = new StringContent(body, Encoding.UTF8, "application/json"),
		};

	private static string OpenIdConfiguration() =>
		$$"""
		{
		  "token_endpoint": "{{Authority}}oauth2/v2.0/token",
		  "authorization_endpoint": "{{Authority}}oauth2/v2.0/authorize",
		  "issuer": "https://login.microsoftonline.com/{{TenantId}}/v2.0",
		  "userinfo_endpoint": "https://graph.microsoft.com/oidc/userinfo",
		  "end_session_endpoint": "{{Authority}}oauth2/v2.0/logout",
		  "response_modes_supported": [ "query", "fragment", "form_post" ],
		  "response_types_supported": [ "code", "id_token", "code id_token", "id_token token" ],
		  "scopes_supported": [ "openid", "profile", "email", "offline_access" ],
		  "subject_types_supported": [ "pairwise" ],
		  "id_token_signing_alg_values_supported": [ "RS256" ],
		  "token_endpoint_auth_methods_supported": [ "client_secret_post", "private_key_jwt", "client_secret_basic" ]
		}
		""";

	private string TokenResponse()
	{
		var accessToken = $"stub-access-token-{++_tokenCounter}";
		IssuedAccessTokens.Add(accessToken);

		var response = new StubTokenResponse
		{
			TokenType = "Bearer",
			Scope = "User.Read",
			ExpiresIn = ExpiresInSeconds,
			ExtExpiresIn = ExpiresInSeconds,
			AccessToken = accessToken,
			RefreshToken = $"stub-refresh-token-{_instanceId}-{_tokenCounter}",
			IdToken = BuildIdToken(),
			ClientInfo = Base64UrlEncode($$"""{"uid":"{{ObjectId}}","utid":"{{TenantId}}"}"""),
		};

		return JsonSerializer.Serialize(response, StubJsonContext.Default.StubTokenResponse);
	}

	/// <summary>
	/// An unsigned JWT. MSAL parses the id_token for account details but does not validate its
	/// signature for public clients, so a fixed placeholder signature is sufficient here.
	/// </summary>
	private static string BuildIdToken()
	{
		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var header = Base64UrlEncode("""{"typ":"JWT","alg":"RS256","kid":"stub"}""");
		var payload = Base64UrlEncode(
			$$"""
			{
			  "aud": "{{ClientId}}",
			  "iss": "https://login.microsoftonline.com/{{TenantId}}/v2.0",
			  "iat": {{now - 60}},
			  "nbf": {{now - 60}},
			  "exp": {{now + 3600}},
			  "sub": "{{ObjectId}}",
			  "oid": "{{ObjectId}}",
			  "tid": "{{TenantId}}",
			  "preferred_username": "{{Username}}",
			  "name": "Test User",
			  "ver": "2.0"
			}
			""");

		return $"{header}.{payload}.c3R1Yi1zaWduYXR1cmU";
	}

	private static string Base64UrlEncode(string value) =>
		Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');

	/// <summary>
	/// MSAL caches the <see cref="HttpClient"/> it gets, so one instance is reused; the handler
	/// routes every request back into <see cref="Handle"/>.
	/// </summary>
	private sealed class StubHttpClientFactory : IMsalHttpClientFactory
	{
		private readonly HttpClient _client;

		public StubHttpClientFactory(StubEntra tenant)
			=> _client = new HttpClient(new StubHandler(tenant));

		public HttpClient GetHttpClient() => _client;
	}

	private sealed class StubHandler : HttpMessageHandler
	{
		private readonly StubEntra _tenant;

		public StubHandler(StubEntra tenant) => _tenant = tenant;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var response = _tenant.Handle(request);
			response.RequestMessage = request;
			return Task.FromResult(response);
		}
	}
}

/// <summary>Shape of the token endpoint response. Serialized through the source-generated context.</summary>
internal sealed record StubTokenResponse
{
	[JsonPropertyName("token_type")]
	public string TokenType { get; init; } = "Bearer";

	[JsonPropertyName("scope")]
	public string Scope { get; init; } = string.Empty;

	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; init; }

	[JsonPropertyName("ext_expires_in")]
	public int ExtExpiresIn { get; init; }

	[JsonPropertyName("access_token")]
	public string AccessToken { get; init; } = string.Empty;

	[JsonPropertyName("refresh_token")]
	public string RefreshToken { get; init; } = string.Empty;

	[JsonPropertyName("id_token")]
	public string IdToken { get; init; } = string.Empty;

	[JsonPropertyName("client_info")]
	public string ClientInfo { get; init; } = string.Empty;
}

/// <summary>
/// Source-generated serialization (AGENTS.md §2) - also what keeps this working on the WebAssembly
/// and mobile heads, where reflection-based serialization is trimmed away.
/// </summary>
[JsonSerializable(typeof(StubTokenResponse))]
internal partial class StubJsonContext : JsonSerializerContext
{
}
