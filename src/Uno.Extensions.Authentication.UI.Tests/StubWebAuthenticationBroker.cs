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
/// where the built-in broker throws <see cref="NotImplementedException"/> (spec 012 F8).
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

	/// <summary>
	/// When set, the next authentication completes with this status (e.g.
	/// <see cref="WebAuthenticationStatus.UserCancel"/>) instead of succeeding, then resets.
	/// </summary>
	public WebAuthenticationStatus? NextStatus { get; set; }

	/// <summary>The most recently minted access token.</summary>
	public string LastAccessToken => _lastAccessToken
		?? throw new InvalidOperationException("No token has been issued yet.");

	/// <summary>
	/// Registers the stub with <see cref="ApiExtensibility"/>. Must run before anything touches
	/// <see cref="WebAuthenticationBroker"/>'s static constructor, which resolves and caches the
	/// provider exactly once - the harness calls this at creation, before any broker use.
	/// Registration is idempotent and first-wins, so repeated calls are no-ops and the product's
	/// own (runtime-gated) desktop registration is displaced in this process.
	/// </summary>
	public static void EnsureRegistered() =>
		ApiExtensibility.Register(
			typeof(IWebAuthenticationBrokerProvider),
			_ => Instance);

	public void Reset()
	{
		InvocationCount = 0;
		LastRequestUri = null;
		NextStatus = null;
	}

	public Uri GetCurrentApplicationCallbackUri() => new("web-tests://callback");

	public Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri, Uri callbackUri, CancellationToken ct)
	{
		InvocationCount++;
		LastRequestUri = requestUri;
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
		return Task.FromResult(new WebAuthenticationResult(response, 0, WebAuthenticationStatus.Success));
	}
}
