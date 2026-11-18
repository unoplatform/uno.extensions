using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Uno.Extensions.Http;

namespace Uno.Extensions;

/// <summary>
/// Extensions that let an app take part in an Aspire-orchestrated solution.
/// </summary>
public static class HostBuilderExtensions
{
	private const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

	/// <summary>
	/// Adds service discovery and the standard resilience handler to every <see cref="HttpClient"/>,
	/// and exports OpenTelemetry when an OTLP endpoint is configured.
	/// </summary>
	/// <param name="builder">The host builder to configure.</param>
	/// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
	public static IHostBuilder UseServiceDefaults(this IHostBuilder builder)
	{
		// Checked at call time, not in ConfigureServices: client registrations read the key whatever the call order.
		if (builder.IsRegistered(HttpRetryOwnership.ResilienceHandlerKey))
		{
			return builder;
		}

		return builder.ConfigureServices((context, services) =>
		{
			services.AddServiceDiscovery();
			services.ConfigureHttpClientDefaults(http =>
			{
				http.AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
				http.AddServiceDiscovery();
			});

			// Inline so the linker folds it and drops the OpenTelemetry path on WebAssembly.
			if (OperatingSystem.IsBrowser())
			{
				return;
			}

			if (!string.IsNullOrWhiteSpace(context.Configuration[OtlpEndpointKey]))
			{
				AddOpenTelemetry(services);
			}
		});
	}

	private static void AddOpenTelemetry(IServiceCollection services)
	{
		services.AddLogging(logging => logging.AddOpenTelemetry(options =>
		{
			options.IncludeFormattedMessage = true;
			options.IncludeScopes = true;
		}));

		services.AddOpenTelemetry()
			.WithMetrics(metrics => metrics.AddHttpClientInstrumentation().AddRuntimeInstrumentation())
			.WithTracing(tracing => tracing.AddHttpClientInstrumentation())
			.UseOtlpExporter();
	}
}
