namespace Uno.Extensions.Hosting;

/// <summary>
/// Contains configuration for the app host feature.
/// </summary>
public class HostConfiguration
{
	/// <summary>
	/// Gets or sets the prefix used in app configuration file names. (e.g. "appsettings.{prefix}.json")
	/// </summary>
	public string? AppConfigPrefix { get; set; }
	/// <summary>
	/// Gets or sets the URL to navigate to when the app starts.
	/// </summary>
	public string? LaunchUrl { get; set; }
}
