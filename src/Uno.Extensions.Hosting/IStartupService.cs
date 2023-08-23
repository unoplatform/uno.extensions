namespace Uno.Extensions.Hosting;

/// <summary>
/// Implemented by services that are required to complete startup prior to the first navigation.
/// </summary>
public interface IStartupService
{
	/// <summary>
	/// Called when the application has completed startup.
	/// </summary>
	/// <returns>
	/// A task that represents the startup process.
	/// </returns>
	Task StartupComplete();
}
