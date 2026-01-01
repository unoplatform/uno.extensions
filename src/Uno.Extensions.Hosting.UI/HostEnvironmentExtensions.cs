namespace Uno.Extensions.Hosting;

/// <summary>
/// Extension methods for the IHostEnvironment type
/// </summary>
public static class HostEnvironmentExtensions
{
	/// <summary>
	/// Creats an IAppHostEnvironment from an IHostEnvironment
	/// </summary>
	/// <param name="host">The source IHostEnvironment</param>
	/// <param name="appDataPath">The app data path</param>
	/// <param name="hostAssembly">The host assembly</param>
	/// <returns></returns>
	public static IAppHostEnvironment FromHostEnvironment(this IHostEnvironment host, string? appDataPath, Assembly? hostAssembly)
	{
		return new AppHostingEnvironment
		{
			AppDataPath = appDataPath,
			ApplicationName = host.ApplicationName,
			ContentRootFileProvider = host.ContentRootFileProvider,
			ContentRootPath = host.ContentRootPath,
			EnvironmentName = host.EnvironmentName,
			HostAssembly = hostAssembly
		};
	}
}
