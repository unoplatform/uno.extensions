using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Hosting;

/// <summary>
/// Interface that extends the IHostEnvironment with app specific properties for an application
/// </summary>
public interface IAppHostEnvironment : IHostEnvironment
{
	/// <summary>
	/// Gets a path where app data can be stored
	/// </summary>
	string? AppDataPath { get; }

	/// <summary>
	/// Gets a reference to the host assembly
	/// </summary>
	Assembly? HostAssembly { get; }
}
