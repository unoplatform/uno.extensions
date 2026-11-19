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
	/// <remarks>
	/// Assigning it replaces what an earlier call to <c>UseServiceDefaults</c> set; use <c>+=</c> to keep it.
	/// </remarks>
	public Action<HttpStandardResilienceOptions>? ConfigureResilience { get; set; }

	/// <summary>
	/// Whether OpenTelemetry is exported when an OTLP endpoint is configured. <c>true</c> by default.
	/// </summary>
	public bool UseTelemetry { get; set; } = true;

	/// <summary>
	/// Whether telemetry is exported outside the Development environment too. <c>false</c> by default, so a released
	/// app sends nothing when an OTLP endpoint appears in its configuration on someone else's machine.
	/// </summary>
	public bool ExportTelemetryInAllEnvironments { get; set; }
}
