using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;

namespace Uno.Extensions;

/// <summary>
/// What <see cref="HostBuilderExtensions.UseServiceDefaults(IHostBuilder, Action{ServiceDefaultsOptions})"/> adds to the app.
/// </summary>
public sealed class ServiceDefaultsOptions
{
	/// <summary>
	/// Whether every <see cref="HttpClient"/> gets the standard resilience handler. <c>true</c> by default.
	/// When <c>false</c>, Kiota clients keep their own retries.
	/// </summary>
	public bool UseResilience { get; set; } = true;

	/// <summary>
	/// Changes the standard resilience handler, after unsafe methods are excluded from retries.
	/// </summary>
	public Action<HttpStandardResilienceOptions>? ConfigureResilience { get; set; }

	/// <summary>
	/// Whether OpenTelemetry is exported when an OTLP endpoint is configured. <c>true</c> by default.
	/// </summary>
	public bool UseTelemetry { get; set; } = true;
}
