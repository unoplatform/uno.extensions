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
/// call <see cref="TryRegister"/> themselves during startup, before the broker's first use. The
/// type only exists in the non-WinAppSDK builds of this package: guard direct references with
/// <c>#if !WINDOWS</c> in code shared with a WinAppSDK head.
/// </para>
/// <para>
/// The redirect URI must be a loopback HTTP address (for example
/// <c>http://localhost:{port}/authentication-callback</c>) registered with the identity provider.
/// Query-string responses (authorization-code flows) and <c>response_mode=form_post</c> responses
/// complete in a single request. Responses on the URL fragment (implicit-style flows) are
/// supported through a relay: browsers never send fragments to a server, so a bare callback hit
/// is answered with a static page whose script re-requests the callback carrying the fragment,
/// and the result is handed back in its original fragment shape.
/// <see cref="GetCurrentApplicationCallbackUri"/> picks a free port on first use and keeps it for
/// the process lifetime, which requires the identity provider to allow variable-port loopback
/// redirects (RFC 8252 mandates this; Microsoft Entra and Duende IdentityServer honor it) —
/// configure an explicit redirect URI to pin the port instead.
/// </para>
/// <para>
/// The listener accepts the first request to the callback path from any page in the system
/// browser or any local process; it cannot know which request it started. Flows that carry no
/// protocol-level binding of their own (the Web provider's plain token-on-redirect shape) should
/// bind the response with an OAuth <c>state</c> value - see the Web provider's <c>{State}</c>
/// placeholder.
/// </para>
/// </remarks>
public class DesktopWebAuthenticationBrokerProvider : IWebAuthenticationBrokerProvider
{
	/// <summary>
	/// The <see cref="WebAuthenticationResult.ResponseErrorDetail"/> reported alongside
	/// <see cref="WebAuthenticationStatus.UserCancel"/> when the broker's own
	/// <see cref="WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout"/>
	/// elapsed rather than the user backing out. WinRT has no distinct timeout status, so this is
	/// how callers tell the two apart.
	/// </summary>
	public const uint TimeoutErrorDetail = (uint)HttpStatusCode.RequestTimeout;

	// Static response page only: reflecting anything from the callback request into the response
	// would be a reflected-XSS vector on a page served from localhost.
	private const string CompletionHtml =
		"""<!DOCTYPE html><html><head><meta charset="utf-8"><title>Sign-in complete</title></head><body><p>Sign-in complete. You can close this window and return to the app.</p></body></html>""";

	/// <summary>Marks a relayed fragment on the second callback request; see <see cref="FragmentRelayHtml"/>.</summary>
	private const string RelayedFragmentQueryPrefix = "?uno-fragment=1&";

	/// <summary>The second request when the redirect carried neither query nor fragment.</summary>
	private const string NoFragmentSentinelQuery = "?uno-no-fragment=1";

	// Served when the callback arrives with no response parameters: a fragment (implicit-flow
	// response) never reaches the server, so this page's script re-requests the callback carrying
	// the fragment as a marked query. Static content only - the fragment travels in the browser's
	// own request and is never reflected into HTML. The markers are interpolated from the same
	// constants the parser below matches on, so the two cannot drift apart.
	private const string FragmentRelayHtml =
		$$"""<!DOCTYPE html><html><head><meta charset="utf-8"><title>Signing in…</title></head><body><p>Completing sign-in…</p><script>var h=window.location.hash;window.location.replace(window.location.pathname+(h&&h.length>1?"{{RelayedFragmentQueryPrefix}}"+h.substring(1):"{{NoFragmentSentinelQuery}}"));</script></body></html>""";

	private static readonly byte[] CompletionBody = Encoding.UTF8.GetBytes(CompletionHtml);
	private static readonly byte[] FragmentRelayBody = Encoding.UTF8.GetBytes(FragmentRelayHtml);

	/// <summary>
	/// Built outside DI (Uno's extensibility registry creates it), so this is the ambient logger
	/// <c>UseLogging()</c> installs; <c>NullLogger</c> until then.
	/// </summary>
	private ILogger Logger => this.Log();

	/// <summary>
	/// The loopback port used by <see cref="GetCurrentApplicationCallbackUri"/>: picked free on
	/// first use, then fixed for the process lifetime so login and logout use the same redirect.
	/// A race between probing and binding is possible but the bind failure is loud, not silent.
	/// </summary>
	private static readonly Lazy<int> _defaultPort = new(
		() =>
		{
			using var probe = new TcpListener(IPAddress.Loopback, 0);
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
	/// compile-time gate cannot make this decision. Registration is idempotent and first-wins: an
	/// app that supplies its own <see cref="IWebAuthenticationBrokerProvider"/> must register it
	/// before <c>AddWeb</c>/<c>AddOidc</c> run.
	/// </remarks>
	/// <returns>
	/// <see langword="true"/> when the process is on a desktop OS and the registration was
	/// attempted; <see langword="false"/> on the platforms that keep their native broker.
	/// </returns>
	public static bool TryRegister()
	{
		if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsBrowser())
		{
			return false;
		}

		if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
		{
			ApiExtensibility.Register(
				typeof(IWebAuthenticationBrokerProvider),
				_ => new DesktopWebAuthenticationBrokerProvider());
			return true;
		}

		return false;
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

		if (requestUri.Scheme != Uri.UriSchemeHttp && requestUri.Scheme != Uri.UriSchemeHttps)
		{
			// The request URI goes to the OS's URL handler, which dispatches on scheme: anything but
			// http(s) would reach some other protocol handler rather than a browser.
			throw new ArgumentException(
				$"The desktop web authentication broker only opens http or https request URIs; got '{requestUri.Scheme}:'.",
				nameof(requestUri));
		}

		using var timeout = new CancellationTokenSource(WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout);
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, ct);

		using var listener = new HttpListener();
		// Bind the whole loopback port and match the path manually: HttpListener prefixes need a
		// trailing slash, and the IdP redirects to the exact configured path (no slash) - a prefix
		// of "/authentication-callback/" would never see it. The port is ours alone, so the wider
		// binding adds no exposure beyond the 404 branch below.
		var prefix = $"http://{callbackUri.Host}:{callbackUri.Port}/";
		listener.Prefixes.Add(prefix);
		try
		{
			listener.Start();
		}
		catch (HttpListenerException ex)
		{
			if (Logger.IsEnabled(LogLevel.Error))
			{
				Logger.LogError(ex, "Unable to listen on {Prefix} for the sign-in redirect: the port is in use, or this user may not bind it. Pin a different port through the callback URI, or free the port", prefix);
			}

			throw;
		}

		try
		{
			// Fire the browser and keep listening immediately: launch failures (no browser
			// installed, no shell) throw synchronously from Process.Start and propagate here.
			await LaunchBrowserAsync(requestUri, linked.Token).ConfigureAwait(false);

			while (true)
			{
				HttpListenerContext context;
				// GetContextAsync has no cancellation of its own; WaitAsync abandons the wait and
				// the finally's Stop() faults the abandoned task, which is observed so it never
				// surfaces as an UnobservedTaskException.
				var pending = listener.GetContextAsync();
				try
				{
					context = await pending.WaitAsync(linked.Token).ConfigureAwait(false);
				}
				catch (OperationCanceledException) when (ct.IsCancellationRequested)
				{
					// The caller cancelled: propagate so WebAuthenticationBroker's task cancels.
					Observe(pending);
					throw;
				}
				catch (OperationCanceledException)
				{
					// Only the broker's own AuthenticationTimeout can be left; WinRT has no
					// distinct timeout status, so the detail carries the distinction.
					Observe(pending);
					if (Logger.IsEnabled(LogLevel.Warning))
					{
						Logger.LogWarning("No redirect reached {Callback} within the broker's AuthenticationTimeout ({Timeout}); reporting the flow as cancelled", callbackUri, WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout);
					}

					return new WebAuthenticationResult(null, TimeoutErrorDetail, WebAuthenticationStatus.UserCancel);
				}

				if (!string.Equals(context.Request.Url?.AbsolutePath, callbackUri.AbsolutePath, StringComparison.OrdinalIgnoreCase))
				{
					if (Logger.IsEnabled(LogLevel.Debug))
					{
						Logger.LogDebug("Ignoring a request to {Path} on the sign-in listener; waiting for {CallbackPath}", context.Request.Url?.AbsolutePath, callbackUri.AbsolutePath);
					}

					TryClose(context.Response, HttpStatusCode.NotFound);
					continue;
				}

				var query = await ReadResponseQueryAsync(context.Request, linked.Token).ConfigureAwait(false);

				if (IsBare(query, callbackUri))
				{
					// The response may be riding the URL fragment (implicit flows), which browsers
					// never send to a server. Serve the relay page, whose script re-requests this
					// callback with the fragment as a marked query - or the no-fragment sentinel -
					// and keep listening for that second request (spec 013 F12). A write the
					// browser dropped is not fatal either: it retries, or the timeout ends the flow.
					await TryRespondAsync(context.Response, FragmentRelayBody, linked.Token).ConfigureAwait(false);
					continue;
				}

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
				var result = new WebAuthenticationResult(responseData, 0, WebAuthenticationStatus.Success);

				// The sign-in is complete at this point; the completion page is a courtesy to the
				// user, and a connection the browser dropped while it was written must not discard
				// the response that already arrived.
				await TryRespondAsync(context.Response, CompletionBody, linked.Token).ConfigureAwait(false);
				return result;
			}
		}
		finally
		{
			listener.Stop();
		}
	}

	/// <summary>
	/// The response parameters carried by a callback request: the query string, or the body of a
	/// <c>response_mode=form_post</c> POST in query form, so both shapes complete the flow.
	/// </summary>
	private static async Task<string> ReadResponseQueryAsync(HttpListenerRequest request, CancellationToken ct)
	{
		if (request.HttpMethod == "POST" &&
			request.HasEntityBody &&
			request.ContentType?.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true)
		{
			using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
			var body = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
			return body.Length == 0 ? string.Empty : "?" + body;
		}

		return request.Url?.Query ?? string.Empty;
	}

	/// <summary>
	/// Whether a callback request carries no response parameters beyond the callback's own
	/// configured query - the shape a fragment response (never sent to a server) arrives in.
	/// </summary>
	private static bool IsBare(string query, Uri callbackUri) =>
		string.Equals(query.TrimStart('?'), callbackUri.Query.TrimStart('?'), StringComparison.Ordinal);

	private static void Observe(Task task) =>
		_ = task.ContinueWith(
			static t => _ = t.Exception,
			CancellationToken.None,
			TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
			TaskScheduler.Default);

	private async Task<bool> TryRespondAsync(HttpListenerResponse response, byte[] body, CancellationToken ct)
	{
		try
		{
			response.ContentType = "text/html; charset=utf-8";
			response.ContentLength64 = body.Length;
			await response.OutputStream.WriteAsync(body, ct).ConfigureAwait(false);
			response.Close();
			return true;
		}
		catch (Exception ex) when (ex is HttpListenerException or IOException or ObjectDisposedException)
		{
			if (Logger.IsEnabled(LogLevel.Debug))
			{
				Logger.LogDebug(ex, "The browser dropped the connection before the response page was written");
			}

			response.Abort();
			return false;
		}
	}

	private void TryClose(HttpListenerResponse response, HttpStatusCode status)
	{
		try
		{
			response.StatusCode = (int)status;
			response.Close();
		}
		catch (Exception ex) when (ex is HttpListenerException or IOException or ObjectDisposedException)
		{
			if (Logger.IsEnabled(LogLevel.Debug))
			{
				Logger.LogDebug(ex, "The browser dropped the connection before the {Status} response was written", status);
			}

			response.Abort();
		}
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
		// The launcher's Process handle is not needed once the browser is open.
		if (OperatingSystem.IsWindows())
		{
			Process.Start(new ProcessStartInfo(requestUri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
		}
		else if (OperatingSystem.IsMacOS())
		{
			Process.Start(new ProcessStartInfo("open") { ArgumentList = { requestUri.AbsoluteUri } })?.Dispose();
		}
		else if (OperatingSystem.IsLinux())
		{
			Process.Start(new ProcessStartInfo("xdg-open") { ArgumentList = { requestUri.AbsoluteUri } })?.Dispose();
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
