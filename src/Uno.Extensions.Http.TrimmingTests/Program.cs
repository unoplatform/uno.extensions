using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Refit;
using Uno.Extensions;
using Uno.Extensions.Http;
using Uno.Extensions.Http.Kiota;

const string BaseUrl = "http://trimming.test";
const string ApiVersion = "v2";

DerivedEndpointOptions? derivedEndpoint = null;

// The endpoint sections an app would have in appsettings.json. UseNativeHandler is left at its
// default (true), so the primary handler comes from DI: RecordingHandler below.
var host = Host.CreateDefaultBuilder(args)
	.ConfigureLogging(logging => logging.ClearProviders())
	.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
	{
		["TrimmingRefitApi:Url"] = BaseUrl,
		["TrimmingKiotaClient:Url"] = BaseUrl,
		["TrimmingDerivedApi:Url"] = BaseUrl,
		["TrimmingDerivedApi:ApiVersion"] = ApiVersion,
	}))
	.ConfigureServices((context, services) =>
	{
		services.AddSingleton<RecordingHandler>();
		services.AddTransient<HttpMessageHandler>(sp => sp.GetRequiredService<RecordingHandler>());

		services.AddRefitClient<ITrimmingRefitApi>(context);
		services.AddKiotaClient<TrimmingKiotaClient>(context, name: "TrimmingKiotaClient");

		// A derived endpoint type: its own properties must survive trimming too, not only EndpointOptions'.
		services.AddRefitClientWithEndpoint<ITrimmingDerivedApi, DerivedEndpointOptions>(
			context,
			configure: (builder, endpoint) =>
			{
				derivedEndpoint = endpoint;
				return builder;
			});
	})
	.Build();

var failures = 0;

// Refit: without a BaseAddress the call throws before reaching the handler.
try
{
	var recorder = host.Services.GetRequiredService<RecordingHandler>();
	await host.Services.GetRequiredService<ITrimmingRefitApi>().Get();
	Check("AddRefitClient", recorder.LastRequestUri?.ToString(), $"{BaseUrl}/api/test");
}
catch (InvalidOperationException ex)
{
	// Refit's "BaseAddress must be set on the HttpClient instance".
	Fail("AddRefitClient", $"{ex.Message} (EndpointOptions was not bound: its setters were trimmed)");
}
catch (Exception ex)
{
	Fail("AddRefitClient", $"{ex.GetType().Name}: {ex.Message}");
}

// Kiota: the request adapter takes its BaseUrl from the HttpClient's BaseAddress.
try
{
	var client = host.Services.GetRequiredService<TrimmingKiotaClient>();
	Check("AddKiotaClient", client.Adapter.BaseUrl, BaseUrl);
}
catch (Exception ex)
{
	Fail("AddKiotaClient", $"{ex.GetType().Name}: {ex.Message}");
}

Check("AddRefitClientWithEndpoint (Url)", derivedEndpoint?.Url, BaseUrl);
Check("AddRefitClientWithEndpoint (derived property)", derivedEndpoint?.ApiVersion, ApiVersion);

Console.WriteLine(failures == 0 ? "All trimming checks passed." : $"{failures} trimming check(s) failed.");
return failures == 0 ? 0 : 1;

void Check(string name, string? actual, string expected)
{
	if (string.Equals(actual?.TrimEnd('/'), expected, StringComparison.Ordinal))
	{
		Console.WriteLine($"PASS {name}: {actual}");
	}
	else
	{
		Fail(name, $"expected '{expected}', got '{actual ?? "<null>"}' (EndpointOptions was not bound: its setters were trimmed)");
	}
}

void Fail(string name, string message)
{
	failures++;
	Console.WriteLine($"FAIL {name}: {message}");
}

public interface ITrimmingRefitApi
{
	[Get("/api/test")]
	Task<string> Get();
}

public interface ITrimmingDerivedApi
{
	[Get("/api/test")]
	Task<string> Get();
}

public class DerivedEndpointOptions : EndpointOptions
{
	public string? ApiVersion { get; set; }
}

public class TrimmingKiotaClient(IRequestAdapter adapter)
{
	public IRequestAdapter Adapter { get; } = adapter;
}

/// <summary>Answers every request locally and remembers the URI it was sent to.</summary>
public sealed class RecordingHandler : HttpMessageHandler
{
	public Uri? LastRequestUri { get; private set; }

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		LastRequestUri = request.RequestUri;
		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });
	}
}
