
namespace Uno.Extensions.Hosting;

/// <summary>
/// Extensions for the IHostEnvironment interface.
/// </summary>
public static class HostEnvironmentExtensions
{
	/// <summary>
	/// Checks whether a specific feature has already been registered in the HostBuilderContext properties.
	/// </summary>
	public static bool IsRegistered(this HostBuilderContext context, string registeredKey, bool newIsRegistered = true)
	{
		return context.Properties.IsRegistered(registeredKey, newIsRegistered);
	}

	/// <summary>
	/// Returns the AppDataPath from the IHostEnvironment if it is an IAppHostEnvironment.
	/// </summary>
	/// <param name="hostEnvironment">The IHostEnvironment to retrieve path from</param>
	/// <returns>Path to application data folder to be used by the application</returns>
	public static string GetAppDataPath(this IHostEnvironment hostEnvironment)
		=> (hostEnvironment as IAppHostEnvironment)?.AppDataPath ?? string.Empty;
}
