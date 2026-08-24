#if !WINDOWS
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Uno.AuthenticationBroker;
using Uno.Foundation.Extensibility;

namespace Uno.Extensions.Authentication;

/// <summary>
/// A <see cref="WebAuthenticationBroker"/> implementation for Skia Desktop (Windows, macOS, Linux),
/// where Uno has no built-in broker: the sign-in flow opens in the system browser and the redirect
/// comes back to a loopback HTTP listener, per RFC 8252 §7.3 (native-app loopback redirects).
/// </summary>
/// <remarks>
/// <para>
/// Registered automatically by <c>AddWeb</c>/<c>AddOidc</c> via <see cref="TryRegister"/> when the
/// process runs on a desktop OS; apps that call <see cref="WebAuthenticationBroker"/> directly can
/// call <see cref="TryRegister"/> themselves during startup, before the broker's first use.
/// </para>
/// <para>
/// The redirect URI must be a loopback HTTP address (for example
/// <c>http://localhost:{port}/authentication-callback</c>) registered with the identity provider.
/// Only query-string responses reach the app (authorization-code flows); a fragment never leaves
/// the browser, so implicit flows cannot work over a loopback redirect.
/// <see cref="GetCurrentApplicationCallbackUri"/> picks a free port on first use and keeps it for
/// the process lifetime, which requires the identity provider to allow variable-port loopback
/// redirects (RFC 8252 mandates this; Microsoft Entra and Duende IdentityServer honor it) —
/// configure an explicit redirect URI to pin the port instead.
/// </para>
/// </remarks>
public class DesktopWebAuthenticationBrokerProvider : IWebAuthenticationBrokerProvider
{
	// Static response page only: reflecting anything from the callback request into the response
	// would be a reflected-XSS vector on a page served from localhost.
	private const string CompletionHtml =
		"""<!DOCTYPE html><html><head><meta charset="utf-8"><title>Sign-in complete</title></head><body><p>Sign-in complete. You can close this window and return to the app.</p></body></html>""";

	/// <summary>Marks a relayed fragment on the second callback request; see <see cref="FragmentRelayHtml"/>.</summary>
	private const string RelayedFragmentQueryPrefix = "?uno-fragment=1&";

	/// <summary>The second request when the redirect carried neither query nor fragment.</summary>
	private const string NoFragmentSentinelQuery = "?uno-no-fragment=1";

	// Served when the callback arrives with no query: a fragment (implicit-flow response) never
	// reaches the server, so this page's script re-requests the callback carrying the fragment as
	// a marked query. Static content only - the fragment travels in the browser's own request and
	// is never reflected into HTML.
	private const string FragmentRelayHtml =
		"""<!DOCTYPE html><html><head><meta charset="utf-8"><title>Signing in…</title></head><body><p>Completing sign-in…</p><script>var h=window.location.hash;window.location.replace(window.location.pathname+(h&&h.length>1?"?uno-fragment=1&"+h.substring(1):"?uno-no-fragment=1"));</script></body></html>""";

	/// <summary>
	/// The loopback port used by <see cref="GetCurrentApplicationCallbackUri"/>: picked free on
	/// first use, then fixed for the process lifetime so login and logout use the same redirect.
	/// A race between probing and binding is possible but the bind failure is loud, not silent.
	/// </summary>
	private static readonly Lazy<int> _defaultPort = new(
		() =>
		{
			var probe = new TcpListener(IPAddress.Loopback, 0);
			probe.Start();
			var port = ((IPEndPoint)probe.LocalEndpoint).Port;
			probe.Stop();
			return port;
		},
		LazyThreadSafetyMode.ExecutionAndPublication);

	/// <summary>
	/// Registers this broker with Uno's extensibility registry when running on a desktop OS.
	/// </summary>
	/// <remarks>
	/// Positive desktop allow-list: Android, iOS, Mac Catalyst and WebAssembly have working native
	/// brokers that must keep winning. This matters because the plain-TFM build of this assembly is
	/// what Uno's runtime-asset selector loads on Skia mobile heads (spec 010's mechanism), so a
	/// compile-time gate cannot make this decision. Registration is idempotent and first-wins.
	/// </remarks>
	public static void TryRegister()
	{
		if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsBrowser())
		{
			return;
		}

		if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
		{
			ApiExtensibility.Register(
				typeof(IWebAuthenticationBrokerProvider),
				_ => new DesktopWebAuthenticationBrokerProvider());
		}
	}

	/// <inheritdoc />
	public Uri GetCurrentApplicationCallbackUri() =>
		WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri
			?? new Uri($"http://localhost:{_defaultPort.Value}{WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultCallbackPath}");

	/// <inheritdoc />
	public async Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri, Uri callbackUri, CancellationToken ct)
	{
		if (!callbackUri.IsLoopback || callbackUri.Scheme != Uri.UriSchemeHttp)
		{
			// Never listen on a non-loopback interface: the callback carries the authorization
			// response, and loopback-HTTP is the only shape RFC 8252 sanctions for native apps.
			throw new ArgumentException(
				$"The desktop web authentication broker requires a loopback HTTP callback URI (e.g. 'http://localhost:{{port}}{WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultCallbackPath}'); got '{callbackUri}'. Register a loopback redirect with the identity provider, or supply a custom IWebAuthenticationBrokerProvider.",
				nameof(callbackUri));
		}

		using var timeout = new CancellationTokenSource(WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout);
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, ct);

		using var listener = new HttpListener();
		// Bind the whole loopback port and match the path manually: HttpListener prefixes need a
		// trailing slash, and the IdP redirects to the exact configured path (no slash) - a prefix
		// of "/authentication-callback/" would never see it. The port is ours alone, so the wider
		// binding adds no exposure beyond the 404 branch below.
		listener.Prefixes.Add($"http://{callbackUri.Host}:{callbackUri.Port}/");
		listener.Start();

		try
		{
			// Fire the browser and keep listening immediately: launch failures (no browser
			// installed, no shell) throw synchronously from Process.Start and propagate here.
			await LaunchBrowserAsync(requestUri, linked.Token);

			while (true)
			{
				HttpListenerContext context;
				try
				{
					// GetContextAsync has no cancellation of its own; WaitAsync abandons the wait
					// and the finally's Stop() releases the binding.
					context = await listener.GetContextAsync().WaitAsync(linked.Token);
				}
				catch (OperationCanceledException) when (ct.IsCancellationRequested)
				{
					// The caller cancelled: propagate so WebAuthenticationBroker's task cancels.
					throw;
				}
				catch (OperationCanceledException)
				{
					// Only the broker's own AuthenticationTimeout can be left; WinRT has no
					// distinct timeout status, and the wasm broker reports the same way.
					return new WebAuthenticationResult(null, 0, WebAuthenticationStatus.UserCancel);
				}

				if (!string.Equals(context.Request.Url?.AbsolutePath, callbackUri.AbsolutePath, StringComparison.OrdinalIgnoreCase))
				{
					context.Response.StatusCode = (int)HttpStatusCode.NotFound;
					context.Response.Close();
					continue;
				}

				var query = context.Request.Url?.Query ?? string.Empty;

				if (string.IsNullOrEmpty(query) || query == "?")
				{
					// The response may be riding the URL fragment (implicit flows), which browsers
					// never send to a server. Serve the relay page, whose script re-requests this
					// callback with the fragment as a marked query - or the no-fragment sentinel -
					// and keep listening for that second request (spec 012 F12).
					await RespondAsync(context.Response, FragmentRelayHtml, linked.Token);
					continue;
				}

				await RespondAsync(context.Response, CompletionHtml, linked.Token);

				// Rebuild from the configured callback rather than echoing the raw request URL, so
				// the result matches what the caller registered. Relayed fragments are restored to
				// their original response shape; the sentinel means the redirect was genuinely bare.
				var callbackBase = callbackUri.GetLeftPart(UriPartial.Path);
				var responseData = query switch
				{
					NoFragmentSentinelQuery => callbackBase,
					_ when query.StartsWith(RelayedFragmentQueryPrefix, StringComparison.Ordinal) =>
						$"{callbackBase}#{query[RelayedFragmentQueryPrefix.Length..]}",
					_ => callbackBase + query,
				};
				return new WebAuthenticationResult(responseData, 0, WebAuthenticationStatus.Success);
			}
		}
		finally
		{
			listener.Stop();
		}
	}

	private static async Task RespondAsync(HttpListenerResponse response, string html, CancellationToken ct)
	{
		var body = Encoding.UTF8.GetBytes(html);
		response.ContentType = "text/html; charset=utf-8";
		response.ContentLength64 = body.Length;
		await response.OutputStream.WriteAsync(body, ct);
		response.Close();
	}

	/// <summary>
	/// Opens the system browser at the authorization request URI. Overridable so tests can drive
	/// the redirect without a real browser.
	/// </summary>
	/// <param name="requestUri">The authorization request to navigate to.</param>
	/// <param name="ct">A cancellation token.</param>
	/// <returns>A task that completes once the browser has been launched (not when sign-in ends).</returns>
	protected virtual Task LaunchBrowserAsync(Uri requestUri, CancellationToken ct)
	{
		// ProcessStartInfo.ArgumentList (not a concatenated argument string) so nothing in the URL
		// is ever interpreted by a shell.
		if (OperatingSystem.IsWindows())
		{
			Process.Start(new ProcessStartInfo(requestUri.AbsoluteUri) { UseShellExecute = true });
		}
		else if (OperatingSystem.IsMacOS())
		{
			Process.Start(new ProcessStartInfo("open") { ArgumentList = { requestUri.AbsoluteUri } });
		}
		else if (OperatingSystem.IsLinux())
		{
			Process.Start(new ProcessStartInfo("xdg-open") { ArgumentList = { requestUri.AbsoluteUri } });
		}
		else
		{
			// Also satisfies CA1416: the explicit OS guards prove Process.Start is never reached
			// on the mobile TFMs this file still compiles for.
			throw new PlatformNotSupportedException(
				"The desktop web authentication broker can only launch a browser on Windows, macOS, or Linux.");
		}

		return Task.CompletedTask;
	}
}
#endif
