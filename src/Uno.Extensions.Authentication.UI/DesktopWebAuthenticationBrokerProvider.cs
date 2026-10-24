#if !WINDOWS
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
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
/// browser or any local process; it cannot know which request it started. OIDC flows are bound
/// by the protocol (<c>state</c>, <c>nonce</c>, PKCE, checked by the OIDC client). The Web
/// provider's plain token-on-redirect shape carries no such binding and this broker adds none:
/// an app using it on desktop must bind the response itself (for example a <c>state</c> value its
/// backend echoes back, checked in <c>PostLogin</c>).
/// </para>
/// <para>
/// Diagnostics go to the ambient logger (<c>UseLogging(enableUnoLogging: true)</c>), since the
/// broker is created by Uno's extensibility registry rather than by DI. A derived broker can
/// supply its own <see cref="ILogger"/> through the protected constructor.
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

	private const string HtmlContentType = MediaTypeNames.Text.Html + "; charset=utf-8";

	/// <summary>
	/// Both pages are served at the token-bearing callback URL (the completion page at the relayed
	/// one), so neither may be cached by the browser.
	/// </summary>
	private const string CacheControlHeader = "Cache-Control";
	private const string CacheControlNoStore = "no-store";

	private static readonly byte[] CompletionBody = Encoding.UTF8.GetBytes(CompletionHtml);
	private static readonly byte[] FragmentRelayBody = Encoding.UTF8.GetBytes(FragmentRelayHtml);

	/// <summary>
	/// The largest <c>response_mode=form_post</c> body read, in characters. Any page in the system
	/// browser can POST to the loopback port, so the read is bounded; an authorization response is
	/// a code, a state and at most an id_token - a few KB. Small enough to stay off the LOH.
	/// </summary>
	private const int MaxFormPostLength = 32 * 1024;

	/// <summary>
	/// How long the "sign-in complete" page may take to write. The response has already arrived by
	/// then, so this only bounds how long a stalled browser can hold the result back.
	/// </summary>
	private static readonly TimeSpan CompletionPageTimeout = TimeSpan.FromSeconds(5);

	/// <summary>
	/// How many ports <see cref="DefaultPort"/> tries before giving up on one that is free for both
	/// address families; <c>localhost</c> may resolve to either, and the listener binds whichever
	/// comes first.
	/// </summary>
	private const int DefaultPortProbeAttempts = 8;

	/// <summary>
	/// The loopback port used by <see cref="GetCurrentApplicationCallbackUri"/>, or 0 before the
	/// first use: picked free, then kept so login and logout use the same redirect - until a bind
	/// on it fails, which forgets it so the next flow picks another instead of failing until the
	/// app restarts. A race between probing and binding is possible but the bind failure is loud.
	/// </summary>
	private static int _defaultPort;

	/// <summary>1 once <see cref="TryRegister"/> has registered this broker.</summary>
	private static int _registered;

	private readonly ILogger? _logger;

	/// <summary>1 while this broker has a flow listening; a second one would only fail to bind.</summary>
	private int _flowInProgress;

	/// <summary>1 once the first flow has been announced in the log.</summary>
	private int _announced;

	/// <summary>
	/// Creates the broker Uno's extensibility registry instantiates on the broker's first use.
	/// </summary>
	public DesktopWebAuthenticationBrokerProvider()
	{
	}

	/// <summary>
	/// Creates a broker that logs through <paramref name="logger"/>: for a derived broker that
	/// carries a DI-provided logger instead of the ambient one.
	/// </summary>
	/// <param name="logger">The logger to use, or <see langword="null"/> for the ambient logger.</param>
	protected DesktopWebAuthenticationBrokerProvider(ILogger? logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// The injected logger, or the ambient one <c>UseLogging(enableUnoLogging: true)</c> installs:
	/// built outside DI (Uno's extensibility registry creates it), so a <c>NullLogger</c> until then.
	/// </summary>
	private ILogger Logger => _logger ?? this.Log();

	private static int DefaultPort()
	{
		var port = Volatile.Read(ref _defaultPort);
		if (port != 0)
		{
			return port;
		}

		port = ProbeFreeLoopbackPort();

		// First writer wins, so concurrent first calls still agree on one port.
		var winner = Interlocked.CompareExchange(ref _defaultPort, port, 0);
		return winner == 0 ? port : winner;
	}

	/// <summary>
	/// A port free on IPv4 loopback and, where the OS has it, on IPv6 loopback too: the listener
	/// binds whichever address <c>localhost</c> resolves to first, so a port free on one family
	/// only would fail the bind.
	/// </summary>
	private static int ProbeFreeLoopbackPort()
	{
		var port = 0;
		for (var attempt = 0; attempt < DefaultPortProbeAttempts; attempt++)
		{
			using var probe = new TcpListener(IPAddress.Loopback, 0);
			probe.Start();
			port = ((IPEndPoint)probe.LocalEndpoint).Port;
			probe.Stop();

			if (!Socket.OSSupportsIPv6 || IsFreeOn(IPAddress.IPv6Loopback, port))
			{
				return port;
			}
		}

		// Every candidate was taken on IPv6: hand out the last one anyway and let the bind
		// failure say so; it forgets the port, so the next flow probes again.
		return port;
	}

	private static bool IsFreeOn(IPAddress address, int port)
	{
		try
		{
			using var probe = new TcpListener(address, port);
			probe.Start();
			probe.Stop();
			return true;
		}
		catch (SocketException)
		{
			return false;
		}
	}

	/// <summary>
	/// Registers this broker with Uno's extensibility registry when running on a desktop OS and
	/// no other <see cref="IWebAuthenticationBrokerProvider"/> has been registered yet.
	/// </summary>
	/// <remarks>
	/// Positive desktop allow-list: Android, iOS, Mac Catalyst and WebAssembly have working native
	/// brokers that must keep winning. This matters because the plain-TFM build of this assembly is
	/// what Uno's runtime-asset selector loads on Skia mobile heads, so a
	/// compile-time gate cannot make this decision. Registration is idempotent and first-wins: an
	/// app that supplies its own <see cref="IWebAuthenticationBrokerProvider"/> must register it
	/// before <c>AddWeb</c>/<c>AddOidc</c> run; one registered afterwards is ignored by the
	/// registry. <see cref="WebAuthenticationBroker"/> resolves its provider once, on first use,
	/// so this must also run before anything touches that type.
	/// </remarks>
	/// <returns>
	/// <see langword="true"/> when this broker is the registered provider (registered now or by an
	/// earlier call); <see langword="false"/> on the platforms that keep their native broker, or
	/// when another provider was registered first and keeps winning.
	/// </returns>
	public static bool TryRegister()
	{
		if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsBrowser())
		{
			return false;
		}

		if (!(OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
		{
			return false;
		}

		if (Volatile.Read(ref _registered) == 1)
		{
			return true;
		}

		if (ApiExtensibility.IsRegistered<IWebAuthenticationBrokerProvider>())
		{
			// The registry keeps the first registration; the app's provider handles sign-in. Said
			// once at Information so a missing loopback broker is explainable from the log.
			var logger = typeof(DesktopWebAuthenticationBrokerProvider).Log();
			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.LogInformation("An IWebAuthenticationBrokerProvider is already registered; the desktop loopback broker is not used");
			}

			return false;
		}

		ApiExtensibility.Register(
			typeof(IWebAuthenticationBrokerProvider),
			_ => new DesktopWebAuthenticationBrokerProvider());
		Volatile.Write(ref _registered, 1);
		return true;
	}

	/// <inheritdoc />
	public Uri GetCurrentApplicationCallbackUri() =>
		WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri
			?? new Uri($"http://localhost:{DefaultPort()}{WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultCallbackPath}");

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

		if (Interlocked.Exchange(ref _flowInProgress, 1) != 0)
		{
			// A double-clicked sign-in button, typically. The second flow could only fail to bind
			// the port the first one holds; say what happened instead.
			if (Logger.IsEnabled(LogLevel.Debug))
			{
				Logger.LogDebug("Rejecting a second sign-in flow while one is listening on {Callback}", callbackUri.GetLeftPart(UriPartial.Path));
			}

			throw new InvalidOperationException("A sign-in or sign-out flow is already in progress on the desktop web authentication broker. Wait for it to complete, or cancel it, before starting another.");
		}

		try
		{
			return await ListenForCallbackAsync(requestUri, callbackUri, ct).ConfigureAwait(false);
		}
		finally
		{
			Volatile.Write(ref _flowInProgress, 0);
		}
	}

	private async Task<WebAuthenticationResult> ListenForCallbackAsync(Uri requestUri, Uri callbackUri, CancellationToken ct)
	{
		if (Logger.IsEnabled(LogLevel.Information) && Interlocked.Exchange(ref _announced, 1) == 0)
		{
			// First-wins registration is otherwise invisible: this line's presence or absence is
			// how to tell whether this broker or an app-supplied one is handling sign-in.
			Logger.LogInformation("WebAuthenticationBroker flows are handled by the desktop loopback broker (system browser + {Callback})", callbackUri.GetLeftPart(UriPartial.Path));
		}

		using var timeout = new CancellationTokenSource(WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout);
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, ct);

		using var listener = new HttpListener();
		// Bind the whole loopback port and match the path manually: HttpListener prefixes need a
		// trailing slash, and the IdP redirects to the exact configured path (no slash) - a prefix
		// of "/authentication-callback/" would never see it. Requests to other paths get a 404
		// below, and so does anything that did not come from the loopback interface (http.sys on
		// Windows routes a hostname prefix by Host header, so the binding alone does not prove it).
		var prefix = $"http://{callbackUri.Host}:{callbackUri.Port}/";
		listener.Prefixes.Add(prefix);
		try
		{
			listener.Start();
		}
		catch (HttpListenerException ex)
		{
			// Forget a default port that would not bind, so the next flow probes for another rather
			// than failing on this one until the app restarts. A pinned port is the app's to fix.
			var wasDefault = Interlocked.CompareExchange(ref _defaultPort, 0, callbackUri.Port) == callbackUri.Port;
			if (Logger.IsEnabled(LogLevel.Error))
			{
				Logger.LogError(ex, "Unable to listen on {Prefix} for the sign-in redirect: the port is in use, or this user may not bind it. {Remedy}", prefix, wasDefault ? "The next sign-in attempt picks a new port (restart the app if the redirect URI was captured at startup)" : "Pin a different port through the callback URI, or free the port");
			}

			throw;
		}

		try
		{
			// Fire the browser and keep listening immediately: launch failures (no browser
			// installed, no shell) propagate here and end the flow.
			try
			{
				await LaunchBrowserAsync(requestUri, linked.Token).ConfigureAwait(false);
			}
			catch (Win32Exception ex)
			{
				// Process.Start's "No such file or directory" names nothing; say which launcher.
				if (Logger.IsEnabled(LogLevel.Error))
				{
					Logger.LogError(ex, "The system browser could not be opened for the sign-in request: {Launcher} failed to start", BrowserLauncherName);
				}

				throw;
			}

			if (Logger.IsEnabled(LogLevel.Debug))
			{
				Logger.LogDebug("Sign-in request opened in the system browser; waiting for the redirect to {Callback}", callbackUri.GetLeftPart(UriPartial.Path));
			}

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
				catch (OperationCanceledException)
				{
					Observe(pending);
					if (ct.IsCancellationRequested)
					{
						// The caller cancelled: propagate so WebAuthenticationBroker's task cancels.
						throw;
					}

					// Only the broker's own AuthenticationTimeout can be left; WinRT has no
					// distinct timeout status, so the detail carries the distinction.
					if (Logger.IsEnabled(LogLevel.Warning))
					{
						Logger.LogWarning("No redirect reached {Callback} within the broker's AuthenticationTimeout ({Timeout}); reporting the flow as cancelled", callbackUri.GetLeftPart(UriPartial.Path), WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout);
					}

					return new WebAuthenticationResult(null, TimeoutErrorDetail, WebAuthenticationStatus.UserCancel);
				}

				if (!IPAddress.IsLoopback(context.Request.RemoteEndPoint.Address))
				{
					if (Logger.IsEnabled(LogLevel.Warning))
					{
						Logger.LogWarning("Ignoring a request to the sign-in listener from a non-loopback address");
					}

					TryClose(context.Response, HttpStatusCode.NotFound);
					continue;
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

				var query = await TryReadResponseQueryAsync(context.Request, linked.Token).ConfigureAwait(false);
				if (query is null)
				{
					// Oversized, aborted or cut off by the timeout: not a response this flow can
					// use, and not a reason to end it - any page can POST here. Keep listening; if
					// the timeout is what cut it off, the next wait ends the flow as a timeout.
					TryClose(context.Response, HttpStatusCode.BadRequest);
					continue;
				}

				if (IsBare(query, callbackUri))
				{
					// The response may be riding the URL fragment (implicit flows), which browsers
					// never send to a server. Serve the relay page, whose script re-requests this
					// callback with the fragment as a marked query - or the no-fragment sentinel -
					// and keep listening for that second request. A write the
					// browser dropped is not fatal either: it retries, or the timeout ends the flow.
					await TryRespondAsync(context.Response, FragmentRelayBody, linked.Token).ConfigureAwait(false);
					continue;
				}

				// Rebuild from the configured callback rather than echoing the raw request URL, so
				// the result matches what the caller registered. Relayed fragments are restored to
				// their original response shape; the sentinel means the redirect was genuinely bare.
				var callbackBase = callbackUri.GetLeftPart(UriPartial.Path);
				var (responseData, shape) = query switch
				{
					NoFragmentSentinelQuery => (callbackBase, "bare redirect"),
					_ when query.StartsWith(RelayedFragmentQueryPrefix, StringComparison.Ordinal) =>
						($"{callbackBase}#{query[RelayedFragmentQueryPrefix.Length..]}", "relayed fragment"),
					_ when context.Request.HttpMethod == HttpMethod.Post.Method => (callbackBase + query, "form post"),
					_ => (callbackBase + query, "query string"),
				};
				var result = new WebAuthenticationResult(responseData, 0, WebAuthenticationStatus.Success);

				if (Logger.IsEnabled(LogLevel.Debug))
				{
					Logger.LogDebug("The sign-in redirect reached {Callback} as a {Shape}", callbackBase, shape);
				}

				// The sign-in is complete at this point; the completion page is a courtesy to the
				// user, and a connection the browser dropped while it was written must not discard
				// the response that already arrived. Nor must the flow's own timeout, so the write
				// gets a short one of its own.
				using var courtesy = new CancellationTokenSource(CompletionPageTimeout);
				await TryRespondAsync(context.Response, CompletionBody, courtesy.Token).ConfigureAwait(false);
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
	/// <returns>
	/// The parameters, or <c>null</c> for a body that is longer than
	/// <see cref="MaxFormPostLength"/> or could not be read to its end.
	/// </returns>
	private async Task<string?> TryReadResponseQueryAsync(HttpListenerRequest request, CancellationToken ct)
	{
		if (request.HttpMethod != HttpMethod.Post.Method ||
			!request.HasEntityBody ||
			request.ContentType?.StartsWith(MediaTypeNames.Application.FormUrlEncoded, StringComparison.OrdinalIgnoreCase) != true)
		{
			return request.Url?.Query ?? string.Empty;
		}

		if (request.ContentLength64 > MaxFormPostLength)
		{
			if (Logger.IsEnabled(LogLevel.Debug))
			{
				Logger.LogDebug("Ignoring a {Length}-byte POST to the sign-in callback; the limit is {Limit}", request.ContentLength64, MaxFormPostLength);
			}

			return null;
		}

		// One character past the limit is how a chunked body, which declares no length, is caught
		// overrunning it. The pool may hand back more than asked for, so the read is sliced to
		// exactly that length or the overrun check would never trip.
		var buffer = ArrayPool<char>.Shared.Rent(MaxFormPostLength + 1);
		try
		{
			using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
			var length = await reader.ReadBlockAsync(buffer.AsMemory(0, MaxFormPostLength + 1), ct).ConfigureAwait(false);
			if (length > MaxFormPostLength)
			{
				return null;
			}

			return length == 0 ? string.Empty : string.Concat("?", buffer.AsSpan(0, length));
		}
		catch (Exception ex) when (ex is HttpListenerException or IOException or ObjectDisposedException or OperationCanceledException)
		{
			if (Logger.IsEnabled(LogLevel.Debug))
			{
				Logger.LogDebug(ex, "A POST to the sign-in callback could not be read to its end");
			}

			return null;
		}
		finally
		{
			ArrayPool<char>.Shared.Return(buffer);
		}
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

	private async Task TryRespondAsync(HttpListenerResponse response, byte[] body, CancellationToken ct)
	{
		try
		{
			response.ContentType = HtmlContentType;
			response.AddHeader(CacheControlHeader, CacheControlNoStore);
			response.ContentLength64 = body.Length;
			await response.OutputStream.WriteAsync(body, ct).ConfigureAwait(false);
			response.Close();
		}
		catch (Exception ex) when (ex is HttpListenerException or IOException or ObjectDisposedException or OperationCanceledException)
		{
			// Cancellation included: a page that was not written is never the flow's outcome. The
			// caller's next wait on the same token is what ends the flow, as a timeout or a cancel.
			if (Logger.IsEnabled(LogLevel.Debug))
			{
				Logger.LogDebug(ex, "The response page was not written: the browser dropped the connection, or the flow ended first");
			}

			response.Abort();
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

	/// <summary>The OS facility the default <see cref="LaunchBrowserAsync"/> hands the URL to; for log lines.</summary>
	private static string BrowserLauncherName =>
		OperatingSystem.IsWindows() ? "the shell (ShellExecute)"
		: OperatingSystem.IsMacOS() ? "open"
		: OperatingSystem.IsLinux() ? "xdg-open"
		: "the OS URL handler";

	/// <summary>
	/// Opens the system browser at the authorization request URI.
	/// </summary>
	/// <remarks>
	/// A supported extension point: override it to show the sign-in page some other way - a
	/// specific browser, a private window, an embedded web view - or to drive the redirect in a
	/// test, then register the derived type with <see cref="ApiExtensibility"/> before
	/// <c>AddWeb</c>/<c>AddOidc</c> run. The loopback listener is already accepting requests when
	/// this is called; an exception thrown here ends the flow.
	/// </remarks>
	/// <param name="requestUri">The authorization request to navigate to.</param>
	/// <param name="ct">A cancellation token.</param>
	/// <returns>A task that completes once the browser has been launched (not when sign-in ends).</returns>
	protected virtual Task LaunchBrowserAsync(Uri requestUri, CancellationToken ct)
	{
		// Process.Start is synchronous and, on a cold default browser, slow enough to stall the
		// click that started the flow: the providers await this on whatever context they run on,
		// usually the UI thread. ProcessStartInfo.ArgumentList (not a concatenated argument
		// string) so nothing in the URL is ever interpreted by a shell. The launcher's Process
		// handle is not needed once the browser is open.
		return Task.Run(
			() =>
			{
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
					// Also satisfies CA1416: the explicit OS guards prove Process.Start is never
					// reached on the mobile TFMs this file still compiles for.
					throw new PlatformNotSupportedException(
						"The desktop web authentication broker can only launch a browser on Windows, macOS, or Linux.");
				}
			},
			ct);
	}
}
#endif
