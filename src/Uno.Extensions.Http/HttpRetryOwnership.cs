namespace Uno.Extensions.Http;

/// <summary>
/// Host builder property that tells client registrations a resilience handler already retries requests.
/// Read across separately versioned packages: do not rename the key or the member.
/// </summary>
internal static class HttpRetryOwnership
{
	/// <summary>
	/// Key set to <c>true</c> in <see cref="IHostBuilder.Properties"/> when a resilience handler owns retries.
	/// </summary>
	internal const string ResilienceHandlerKey = "Uno.Extensions.Http.ResilienceHandlerOwnsRetries";

	/// <summary>
	/// Whether a resilience handler owns retries for the clients of this host.
	/// </summary>
	internal static bool IsOwnedByResilienceHandler(HostBuilderContext context) =>
		context.Properties.TryGetValue(ResilienceHandlerKey, out var value) && value is true;
}
