#nullable enable
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Uno.AuthenticationBroker;
using Uno.Foundation.Extensibility;
using Windows.Security.Authentication.Web;

[assembly: ApiExtension(typeof(IWebAuthenticationBrokerProvider), typeof(Uno.Extensions.Authentication.SkiaWebAuthenticationBrokerProvider))]

namespace Uno.Extensions.Authentication;

/// <summary>
/// Web Authentication Broker provider implementation for Skia desktop targets.
/// Uses a local HTTP listener (loopback) to capture OAuth callbacks.
/// </summary>
public class SkiaWebAuthenticationBrokerProvider : IWebAuthenticationBrokerProvider
{
	private const string LoopbackCallbackPath = "/authentication-callback";

	private readonly Lazy<int> _port;

	/// <summary>
	/// Gets or sets a delegate that provides the HTML content for the browser response page.
	/// The boolean parameter indicates whether authentication was successful.
	/// </summary>
	public static Func<bool, string> ResponseHtmlProvider { get; set; } = GetDefaultResponseHtml;

	/// <summary>
	/// Gets or sets a delegate used to open the system browser for authentication.
	/// Can be overridden for testing or custom browser launch behavior.
	/// Returns true if the browser was opened successfully.
	/// </summary>
	public static Func<Uri, bool>? BrowserLauncher { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SkiaWebAuthenticationBrokerProvider"/> class.
	/// </summary>
	public SkiaWebAuthenticationBrokerProvider()
	{
		_port = new Lazy<int>(GetAvailablePort);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SkiaWebAuthenticationBrokerProvider"/> class.
	/// Constructor with owner parameter for API extension compatibility.
	/// </summary>
	/// <param name="owner">The owner object (required by ApiExtensibility pattern).</param>
	public SkiaWebAuthenticationBrokerProvider(object? owner) : this()
	{
	}

	/// <inheritdoc/>
	public Uri GetCurrentApplicationCallbackUri()
	{
		return new Uri($"http://localhost:{_port.Value}{LoopbackCallbackPath}");
	}

	/// <inheritdoc/>
	public async Task<WebAuthenticationResult> AuthenticateAsync(
		WebAuthenticationOptions options,
		Uri requestUri,
		Uri callbackUri,
		CancellationToken ct)
	{
		var port = callbackUri.IsDefaultPort ? _port.Value : callbackUri.Port;
		var callbackPath = callbackUri.AbsolutePath;

		// Listen on root to avoid trailing-slash mismatches with IdP redirects
		var listenerPrefix = $"http://localhost:{port}/";

		using var listener = new HttpListener();
		listener.Prefixes.Add(listenerPrefix);

		try
		{
			listener.Start();
		}
		catch (HttpListenerException ex)
		{
			return new WebAuthenticationResult(
				$"Failed to start local HTTP listener: {ex.Message}",
				(uint)ex.ErrorCode,
				WebAuthenticationStatus.ErrorHttp);
		}

		// If the callback port changed, update redirect_uri in the request
		var actualCallbackUri = new UriBuilder(callbackUri) { Port = port, Host = "localhost", Scheme = "http" }.Uri;
		var modifiedRequestUri = UpdateRedirectUri(requestUri, actualCallbackUri);

		// Open the browser with the authentication URL
		var openBrowser = BrowserLauncher ?? TryOpenBrowser;
		if (!openBrowser(modifiedRequestUri))
		{
			listener.Stop();
			return new WebAuthenticationResult(
				"Failed to open the system browser for authentication.",
				0,
				WebAuthenticationStatus.ErrorHttp);
		}

		try
		{
			while (true)
			{
				HttpListenerContext context;
				try
				{
					context = await listener.GetContextAsync().WaitAsync(ct);
				}
				catch (OperationCanceledException)
				{
					listener.Stop();
					return new WebAuthenticationResult(
						null,
						0,
						WebAuthenticationStatus.UserCancel);
				}
				var request = context.Request;
				var url = request.Url;

				// Filter non-callback requests (e.g. favicon.ico, non-GET)
				var requestPath = url?.AbsolutePath?.TrimEnd('/');
				var expectedPath = callbackPath.TrimEnd('/');
				if (url is null ||
					!string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) ||
					!string.Equals(requestPath, expectedPath, StringComparison.OrdinalIgnoreCase))
				{
					context.Response.StatusCode = (int)HttpStatusCode.NotFound;
					context.Response.Close();
					continue;
				}

				var responseUrl = url.ToString();
				await SendBrowserResponse(context.Response, success: true);
				listener.Stop();

				return new WebAuthenticationResult(
					responseUrl,
					0,
					WebAuthenticationStatus.Success);
			}
		}
		catch (ObjectDisposedException)
		{
			return new WebAuthenticationResult(
				null,
				0,
				WebAuthenticationStatus.UserCancel);
		}
		catch (HttpListenerException) when (ct.IsCancellationRequested)
		{
			return new WebAuthenticationResult(
				null,
				0,
				WebAuthenticationStatus.UserCancel);
		}
		catch (Exception ex)
		{
			return new WebAuthenticationResult(
				ex.Message,
				0,
				WebAuthenticationStatus.ErrorHttp);
		}
	}

	private static int GetAvailablePort()
	{
		// Find an available port by binding to port 0
		using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		var port = ((IPEndPoint)socket.LocalEndPoint!).Port;
		return port;
	}

	private static Uri UpdateRedirectUri(Uri requestUri, Uri actualCallbackUri)
	{
		var query = HttpUtility.ParseQueryString(requestUri.Query);
		var currentRedirect = query["redirect_uri"];
		if (currentRedirect is null)
		{
			return requestUri;
		}

		var actualCallbackString = actualCallbackUri.ToString().TrimEnd('/');
		if (string.Equals(currentRedirect.TrimEnd('/'), actualCallbackString, StringComparison.OrdinalIgnoreCase))
		{
			return requestUri;
		}

		query["redirect_uri"] = actualCallbackString;
		return new UriBuilder(requestUri) { Query = query.ToString() }.Uri;
	}

	private static bool TryOpenBrowser(Uri uri)
	{
		try
		{
			var url = uri.ToString();

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// Use rundll32 to open URL on Windows
				Process.Start(new ProcessStartInfo
				{
					FileName = "rundll32",
					Arguments = $"url.dll,FileProtocolHandler {url}",
					UseShellExecute = false,
					CreateNoWindow = true
				});
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				// Try xdg-open on Linux
				Process.Start(new ProcessStartInfo
				{
					FileName = "xdg-open",
					Arguments = url,
					UseShellExecute = false,
					CreateNoWindow = true
				});
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				// Use open on macOS
				Process.Start(new ProcessStartInfo
				{
					FileName = "open",
					Arguments = url,
					UseShellExecute = false,
					CreateNoWindow = true
				});
			}
			else
			{
				// Fallback: try UseShellExecute
				Process.Start(new ProcessStartInfo
				{
					FileName = url,
					UseShellExecute = true
				});
			}

			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static async Task SendBrowserResponse(HttpListenerResponse response, bool success)
	{
		var html = ResponseHtmlProvider(success);
		var buffer = Encoding.UTF8.GetBytes(html);
		response.ContentType = "text/html; charset=utf-8";
		response.ContentLength64 = buffer.Length;
		response.StatusCode = 200;

		try
		{
			await response.OutputStream.WriteAsync(buffer);
			response.OutputStream.Close();
		}
		catch
		{
			// Ignore errors when sending response
		}
	}

	private static string GetDefaultResponseHtml(bool success)
	{
		return success
			? """
			<!DOCTYPE html>
			<html>
			<head>
				<title>Authentication Complete</title>
				<style>
					body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
						   display: flex; justify-content: center; align-items: center; 
						   height: 100vh; margin: 0; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
					.container { text-align: center; background: white; padding: 40px 60px; 
								 border-radius: 10px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); }
					h1 { color: #28a745; margin-bottom: 10px; }
					p { color: #666; }
					.checkmark { font-size: 48px; margin-bottom: 20px; }
				</style>
			</head>
			<body>
				<div class="container">
					<div class="checkmark">✓</div>
					<h1>Authentication Successful</h1>
					<p>You can close this window and return to the application.</p>
				</div>
				<script>setTimeout(function() { window.close(); }, 3000);</script>
			</body>
			</html>
			"""
			: """
			<!DOCTYPE html>
			<html>
			<head>
				<title>Authentication Failed</title>
				<style>
					body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
						   display: flex; justify-content: center; align-items: center; 
						   height: 100vh; margin: 0; background: linear-gradient(135deg, #e74c3c 0%, #c0392b 100%); }
					.container { text-align: center; background: white; padding: 40px 60px; 
								 border-radius: 10px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); }
					h1 { color: #dc3545; margin-bottom: 10px; }
					p { color: #666; }
					.icon { font-size: 48px; margin-bottom: 20px; }
				</style>
			</head>
			<body>
				<div class="container">
					<div class="icon">✗</div>
					<h1>Authentication Failed</h1>
					<p>Please close this window and try again.</p>
				</div>
			</body>
			</html>
			""";
	}
}
