namespace Uno.Extensions.Http;

/// <summary>
/// Represents endpoint options loaded from appsettings
/// </summary>
public class EndpointOptions
{
	/// <summary>
	/// Gets/Sets the Url for the endpoint
	/// </summary>
	public string? Url { get; set; }

	/// <summary>
	/// Gets/Sets whether to use the native HttpMessageHandler
	/// (defaults to true if not specified)
	/// </summary>
	public bool? UseNativeHandler { get; set; }
}
