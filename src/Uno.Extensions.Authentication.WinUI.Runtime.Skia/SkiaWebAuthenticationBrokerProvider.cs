#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
	private const int DefaultListenerPort = 0; // Use any available port
	private const string LoopbackCallbackPath = "/authentication-callback";

	/// <summary>
	/// Initializes a new instance of the <see cref="SkiaWebAuthenticationBrokerProvider"/> class.
	/// </summary>
	public SkiaWebAuthenticationBrokerProvider()
	{
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
		// For desktop Skia, we use a loopback address with a dynamic port
		// The actual port will be determined when AuthenticateAsync is called
		return new Uri($"http://127.0.0.1{LoopbackCallbackPath}");
	}

	/// <inheritdoc/>
	public async Task<WebAuthenticationResult> AuthenticateAsync(
		WebAuthenticationOptions options,
		Uri requestUri,
		Uri callbackUri,
		CancellationToken ct)
	{
		// Determine the port to use - if callback specifies a port use that, otherwise use any available
		var port = callbackUri.IsDefaultPort ? GetAvailablePort() : callbackUri.Port;
		var listenerUri = BuildListenerUri(callbackUri, port);

		// Update the request URI to use our actual callback URL with port
		var actualCallbackUri = BuildActualCallbackUri(callbackUri, port);
		var modifiedRequestUri = UpdateRequestUriWithCallback(requestUri, callbackUri, actualCallbackUri);

		using var listener = new HttpListener();
		listener.Prefixes.Add(listenerUri);

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

		// Open the browser with the authentication URL
		if (!TryOpenBrowser(modifiedRequestUri))
		{
			listener.Stop();
			return new WebAuthenticationResult(
				"Failed to open the system browser for authentication.",
				0,
				WebAuthenticationStatus.ErrorHttp);
		}

		try
		{
			// Wait for the callback
			var contextTask = listener.GetContextAsync();

			// Create a task that completes when cancellation is requested
			var tcs = new TaskCompletionSource<bool>();
			using var registration = ct.Register(() => tcs.TrySetResult(true));

			var completedTask = await Task.WhenAny(contextTask, tcs.Task);

			if (completedTask == tcs.Task)
			{
				// Cancellation was requested
				listener.Stop();
				return new WebAuthenticationResult(
					null,
					0,
					WebAuthenticationStatus.UserCancel);
			}

			var context = await contextTask;
			var responseUrl = context.Request.Url?.ToString() ?? string.Empty;

			// Send a response to the browser
			await SendBrowserResponse(context.Response, success: true);

			listener.Stop();

			return new WebAuthenticationResult(
				responseUrl,
				0,
				WebAuthenticationStatus.Success);
		}
		catch (ObjectDisposedException)
		{
			// Listener was stopped due to cancellation
			return new WebAuthenticationResult(
				null,
				0,
				WebAuthenticationStatus.UserCancel);
		}
		catch (HttpListenerException ex) when (ct.IsCancellationRequested)
		{
			return new WebAuthenticationResult(
				null,
				(uint)ex.ErrorCode,
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

	private static string BuildListenerUri(Uri callbackUri, int port)
	{
		var path = callbackUri.AbsolutePath;
		if (!path.EndsWith("/"))
		{
			path += "/";
		}
		return $"http://127.0.0.1:{port}{path}";
	}

	private static Uri BuildActualCallbackUri(Uri callbackUri, int port)
	{
		var builder = new UriBuilder(callbackUri)
		{
			Host = "127.0.0.1",
			Port = port,
			Scheme = "http"
		};
		return builder.Uri;
	}

	private static Uri UpdateRequestUriWithCallback(Uri requestUri, Uri originalCallback, Uri actualCallback)
	{
		var requestUriString = requestUri.ToString();
		var originalCallbackEncoded = Uri.EscapeDataString(originalCallback.ToString().TrimEnd('/'));
		var originalCallbackUnencoded = originalCallback.ToString().TrimEnd('/');
		var actualCallbackString = actualCallback.ToString().TrimEnd('/');

		// Try to replace the callback URI in the request (both encoded and unencoded forms)
		if (requestUriString.Contains(originalCallbackEncoded))
		{
			requestUriString = requestUriString.Replace(
				originalCallbackEncoded,
				Uri.EscapeDataString(actualCallbackString));
		}
		else if (requestUriString.Contains(originalCallbackUnencoded))
		{
			requestUriString = requestUriString.Replace(
				originalCallbackUnencoded,
				actualCallbackString);
		}
		else
		{
			// Try to find and replace redirect_uri parameter
			var query = HttpUtility.ParseQueryString(requestUri.Query);
			if (query["redirect_uri"] != null)
			{
				query["redirect_uri"] = actualCallbackString;
				var builder = new UriBuilder(requestUri)
				{
					Query = query.ToString()
				};
				return builder.Uri;
			}
		}

		return new Uri(requestUriString);
	}

	private static bool TryOpenBrowser(Uri uri)
	{
		try
		{
			var url = uri.ToString();

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// Use cmd to handle URL opening on Windows
				Process.Start(new ProcessStartInfo
				{
					FileName = "cmd",
					Arguments = $"/c start \"\" \"{url.Replace("&", "^&")}\"",
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
		var html = success
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

		var buffer = Encoding.UTF8.GetBytes(html);
		response.ContentType = "text/html; charset=utf-8";
		response.ContentLength64 = buffer.Length;
		response.StatusCode = 200;

		try
		{
			await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
			response.OutputStream.Close();
		}
		catch
		{
			// Ignore errors when sending response
		}
	}
}
