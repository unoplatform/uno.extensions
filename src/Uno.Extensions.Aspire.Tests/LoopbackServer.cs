using System.Net;
using System.Net.Sockets;

namespace Uno.Extensions.Aspire.Tests;

/// <summary>
/// Answers 200 to every request on a free loopback port. HttpClient only creates a span on a real connection.
/// </summary>
internal sealed class LoopbackServer : IDisposable
{
	private readonly HttpListener _listener = new();

	private LoopbackServer(int port)
	{
		Address = new Uri($"http://localhost:{port}/");
		_listener.Prefixes.Add(Address.ToString());
		_listener.Start();
		_ = ServeAsync();
	}

	public Uri Address { get; }

	public static LoopbackServer Start()
	{
		var probe = new TcpListener(IPAddress.Loopback, 0);
		probe.Start();
		var port = ((IPEndPoint)probe.LocalEndpoint).Port;
		probe.Stop();
		return new LoopbackServer(port);
	}

	public void Dispose() => _listener.Close();

	private async Task ServeAsync()
	{
		try
		{
			while (_listener.IsListening)
			{
				var context = await _listener.GetContextAsync();
				context.Response.StatusCode = (int)HttpStatusCode.OK;
				context.Response.Close();
			}
		}
		catch (HttpListenerException)
		{
			// Listener closed.
		}
		catch (ObjectDisposedException)
		{
			// Listener closed.
		}
	}
}
