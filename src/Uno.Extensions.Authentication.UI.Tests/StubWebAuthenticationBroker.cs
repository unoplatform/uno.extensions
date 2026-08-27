using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.AuthenticationBroker;
using Uno.Foundation.Extensibility;
using Windows.Security.Authentication.Web;

namespace Uno.Extensions.Authentication.UI.Tests;

/// <summary>
/// A stand-in <see cref="WebAuthenticationBroker"/> implementation: completes the interactive flow
/// instantly with canned tokens on the callback URI, so the whole <c>WebAuthenticationProvider</c>
/// pipeline runs with no browser and no network on every head - including the Skia Desktop lane,
/// where the built-in broker throws <see cref="NotImplementedException"/> (spec 013 F8).
/// </summary>
/// <remarks>
/// A process-wide singleton by necessity: <see cref="WebAuthenticationBroker"/> resolves its
/// provider once, in its static constructor, via <see cref="ApiExtensibility"/> - so per-test
/// behavior is driven through mutable state (<see cref="NextStatus"/>) rather than fresh instances,
/// and <see cref="Reset"/> runs at each harness creation.
/// </remarks>
internal sealed class StubWebAuthenticationBroker : IWebAuthenticationBrokerProvider
{
	public static StubWebAuthenticationBroker Instance { get; } = new();

	private int _tokenCounter;
	private string? _lastAccessToken;

	/// <summary>Number of interactive flows started, so tests can assert whether a prompt happened.</summary>
	public int InvocationCount { get; private set; }

	/// <summary>The authorization request URI most recently handed to the broker.</summary>
	public Uri? LastRequestUri { get; private set; }

	/// <summary>The callback URI most recently handed to the broker.</summary>
	public Uri? LastCallbackUri { get; private set; }

	/// <summary>
	/// When set, the next authentication completes with this status (e.g.
	/// <see cref="WebAuthenticationStatus.UserCancel"/>) instead of succeeding, then resets.
	/// </summary>
	public WebAuthenticationStatus? NextStatus { get; set; }

	/// <summary>
	/// When set, <see cref="GetCurrentApplicationCallbackUri"/> throws with this message - what a
	/// real broker does on a platform that has no callback to derive, such as an iOS app whose
	/// Info.plist declares no custom scheme.
	/// </summary>
	public string? CallbackUriError { get; set; }

	/// <summary>
	/// When set, the next successful response carries this <c>state</c> instead of echoing the
	/// request's - what a forged, replayed or stale response looks like. Resets after use.
	/// </summary>
	public string? NextState { get; set; }

	/// <summary>The most recently minted access token.</summary>
	public string LastAccessToken => _lastAccessToken
		?? throw new InvalidOperationException("No token has been issued yet.");

	/// <summary>
	/// Registers the stub with <see cref="ApiExtensibility"/>. Registration is first-wins, so this
	/// must beat the product's own registration - which <c>AddOidc</c>/<c>AddWeb</c> perform while
	/// building a host, i.e. potentially during <em>another suite's</em> tests (the OIDC suite runs
	/// before this one and registered the real desktop broker, which made every Web test drive a
	/// real loopback listener and time out). Hence the module initializer below: it runs when the
	/// test assembly loads, before any suite executes. The harness still calls this per-test as a
	/// no-op belt-and-braces.
	/// </summary>
	public static void EnsureRegistered() =>
		ApiExtensibility.Register(
			typeof(IWebAuthenticationBrokerProvider),
			_ => Instance);

#pragma warning disable CA2255 // ModuleInitializer in a library: deliberate - this test library must
	// pre-empt the product's first-wins ApiExtensibility registration, which another test suite can
	// trigger before any code in this assembly's test classes runs. See EnsureRegistered's remarks.
	[System.Runtime.CompilerServices.ModuleInitializer]
	internal static void RegisterAtLoad() => EnsureRegistered();
#pragma warning restore CA2255

	public void Reset()
	{
		InvocationCount = 0;
		LastRequestUri = null;
		LastCallbackUri = null;
		NextStatus = null;
		CallbackUriError = null;
		NextState = null;
	}

	public Uri GetCurrentApplicationCallbackUri() =>
		CallbackUriError is { Length: > 0 } error
			? throw new InvalidOperationException(error)
			: new Uri("web-tests://callback");

	public Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri, Uri callbackUri, CancellationToken ct)
	{
		InvocationCount++;
		LastRequestUri = requestUri;
		LastCallbackUri = callbackUri;
		ct.ThrowIfCancellationRequested();

		if (NextStatus is { } status && status != WebAuthenticationStatus.Success)
		{
			NextStatus = null;
			return Task.FromResult(new WebAuthenticationResult(
				null,
				status == WebAuthenticationStatus.ErrorHttp ? 500u : 0u,
				status));
		}

		var accessToken = $"stub-access-token-{++_tokenCounter}";
		_lastAccessToken = accessToken;

		// The provider reads tokens from the callback query using WebAuthenticationSettings'
		// key names, which default to the OAuth-standard "access_token"/"refresh_token".
		var separator = string.IsNullOrEmpty(callbackUri.Query) ? "?" : "&";
		var response = $"{callbackUri}{separator}access_token={accessToken}&refresh_token=stub-refresh-token-{_tokenCounter}";

		// Echo the request's state like a real identity provider - or the override, for tests of
		// what happens when the response is not the one the provider asked for.
		var state = NextState ?? GetQueryValue(requestUri.Query, "state");
		NextState = null;
		if (state is not null)
		{
			response += $"&state={Uri.EscapeDataString(state)}";
		}

		return Task.FromResult(new WebAuthenticationResult(response, 0, WebAuthenticationStatus.Success));
	}

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
}
