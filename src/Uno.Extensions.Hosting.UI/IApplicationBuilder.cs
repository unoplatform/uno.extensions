namespace Uno.Extensions.Hosting;

/// <summary>
/// Defines an abstraction for building your application and App Host
/// </summary>
public interface IApplicationBuilder
{
	/// <summary>
	/// Gets the instance of the Application being built
	/// </summary>
	Application App { get; }

	/// <summary>
	/// Gets the startup arguments passed to OnLaunched
	/// </summary>
	LaunchActivatedEventArgs Arguments { get; }

	/// <summary>
	/// Gets the initial startup Window for the Application
	/// </summary>
	Window Window { get; }

	/// <summary>
	/// Adds a configuration delegate for the <see cref="IHostBuilder" />
	/// </summary>
	/// <param name="configureHost">Configuration Delegate</param>
	/// <returns>The <see cref="IApplicationBuilder" /></returns>
	IApplicationBuilder Configure(Action<IHostBuilder> configureHost);

	/// <summary>
	/// Invokes any supplied delegates passed to the <see cref="Configure"/> method
	/// and then calls the internal Build on the <see cref="IHostBuilder" />
	/// </summary>
	/// <returns>The <see cref="IHost" /></returns>
	IHost Build();
}
