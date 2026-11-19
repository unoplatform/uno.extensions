using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;

namespace Uno.Extensions.Aspire.Tests;

internal static class TestHost
{
	public const string ClientName = "test";

	public static IHostBuilder CreateBuilder(params (string Key, string Value)[] configuration) =>
		new HostBuilder().ConfigureAppConfiguration(config =>
			config.AddInMemoryCollection(configuration.Select(c => new KeyValuePair<string, string?>(c.Key, c.Value))));

	/// <summary>
	/// Removes the wait between resilience retries so a failing request completes immediately.
	/// </summary>
	public static IServiceCollection WithoutRetryDelay(this IServiceCollection services) =>
		services.ConfigureAll<HttpStandardResilienceOptions>(options =>
		{
			options.Retry.Delay = TimeSpan.Zero;
			options.Retry.BackoffType = Polly.DelayBackoffType.Constant;
			options.Retry.UseJitter = false;
		});

	public static HttpClient CreateTestClient(this IHost host) =>
		host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName);
}
