using System;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.OidcClient.Browser;

namespace Uno.Extensions.Authentication.Oidc.UI.Tests;

/// <summary>
/// A stand-in for the interactive browser: completes the front channel instantly by echoing the
/// request's <c>state</c> back with a canned authorization code, so OidcClient's CSRF check passes
/// without a window ever opening.
/// </summary>
/// <remarks>
/// Registered in DI <em>after</em> <c>AddOidc</c> so it wins over the
/// <c>WebAuthenticationBroker</c>-backed browser the provider registers (last registration wins
/// for a single-service resolution).
/// </remarks>
internal sealed class StubBrowser : IBrowser
{
	private int _codeCounter;

	/// <summary>Whether the interactive surface was reached at all.</summary>
	public bool WasInvoked { get; private set; }

	public Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
	{
		WasInvoked = true;
		cancellationToken.ThrowIfCancellationRequested();

		var startUri = new Uri(options.StartUrl);
		var state = GetQueryValue(startUri.Query, "state");

		// No state means this is the end-session flow: just land on the post-logout redirect.
		if (state is null or { Length: 0 })
		{
			return Task.FromResult(new BrowserResult
			{
				ResultType = BrowserResultType.Success,
				Response = options.EndUrl,
			});
		}

		var redirectUri = GetQueryValue(startUri.Query, "redirect_uri") ?? options.EndUrl;
		var separator = redirectUri.Contains('?') ? "&" : "?";
		return Task.FromResult(new BrowserResult
		{
			ResultType = BrowserResultType.Success,
			Response = $"{redirectUri}{separator}code=stub-auth-code-{++_codeCounter}&state={Uri.EscapeDataString(state)}",
		});
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
}
