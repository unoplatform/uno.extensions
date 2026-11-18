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
	/// <remarks>
	/// <list type="bullet">
	/// <item><description>The service addresses Aspire passes win over the same keys in the app's settings files.</description></item>
	/// <item><description>Unsafe methods (<c>POST</c>, <c>PUT</c>, <c>PATCH</c>, <c>DELETE</c>) are never retried.</description></item>
	/// <item><description>Kiota clients stop adding their own retries, so a request is retried by one layer only.</description></item>
	/// <item><description>Nothing is exported on WebAssembly, even when an OTLP endpoint is configured.</description></item>
	/// </list>
	/// </remarks>
	/// <param name="builder">The host builder to configure.</param>
	/// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
	public static IHostBuilder UseServiceDefaults(this IHostBuilder builder) =>
		builder.UseServiceDefaults(_ => { });

	/// <summary>
	/// Adds the service defaults as <see cref="UseServiceDefaults(IHostBuilder)"/> does, with the parts and settings
	/// <paramref name="configure"/> chooses.
	/// </summary>
	/// <param name="builder">The host builder to configure.</param>
	/// <param name="configure">Turns the resilience handler or the telemetry off, or changes the resilience handler.</param>
	/// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
	public static IHostBuilder UseServiceDefaults(this IHostBuilder builder, Action<ServiceDefaultsOptions> configure)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(configure);

		if (builder.IsRegistered(nameof(UseServiceDefaults)))
		{
			return builder;
		}

		var options = new ServiceDefaultsOptions();
		configure(options);

		// Set at call time, not in ConfigureServices: client registrations read it whatever the call order.
		if (options.UseResilience)
		{
			builder.Properties[HttpRetryOwnership.ResilienceHandlerKey] = true;
		}

		// A host configuration step runs before every app configuration step, so the source added here comes after
		// the app's settings files whatever the call order.
		builder.ConfigureHostConfiguration(_ => builder.ConfigureAppConfiguration((_, config) =>
			config.AddInMemoryCollection(ServiceAddresses.FromEnvironment())));

		return builder.ConfigureServices((context, services) =>
		{
			services.AddServiceDiscovery();
			services.ConfigureHttpClientDefaults(http =>
			{
				if (options.UseResilience)
				{
					http.AddStandardResilienceHandler(resilience =>
					{
						resilience.Retry.DisableForUnsafeHttpMethods();
						options.ConfigureResilience?.Invoke(resilience);
					});
				}

				http.AddServiceDiscovery();
			});

			// Inline so the linker folds it and drops the OpenTelemetry path on WebAssembly.
			if (OperatingSystem.IsBrowser())
			{
				return;
			}

			if (options.UseTelemetry && !string.IsNullOrWhiteSpace(context.Configuration[OtlpEndpointKey]))
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
