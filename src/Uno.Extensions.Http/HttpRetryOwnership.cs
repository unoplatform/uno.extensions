namespace Uno.Extensions.Http;

/// <summary>
/// Host builder property that tells client registrations a resilience handler already retries requests.
/// </summary>
/// <remarks>
/// Read by separately versioned packages. The key is a constant, so its value is compiled into them: never change it.
/// </remarks>
internal static class HttpRetryOwnership
{
	/// <summary>
	/// Key set to <c>true</c> in <see cref="IHostBuilder.Properties"/> when a resilience handler owns retries.
	/// </summary>
	internal const string ResilienceHandlerKey = "Uno.Extensions.Http.ResilienceHandlerOwnsRetries";
}
