using System.Diagnostics.CodeAnalysis;

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
	/// Gets stateful properties that Extensions can use to work with each other.
	/// </summary>
	IDictionary<object, object> Properties { get; }

	/// <summary>
	/// Adds a configuration delegate for the <see cref="IHostBuilder" />
	/// </summary>
	/// <param name="configureHost">Configuration Delegate</param>
	/// <returns>The <see cref="IApplicationBuilder" /></returns>
	IApplicationBuilder Configure(Action<IHostBuilder> configureHost);

	/// <summary>
	/// Adds a configuration delegate for the <see cref="IHostBuilder" /> and provides the Window instance
	/// </summary>
	/// <param name="configureHost">Configuration Delegate</param>
	/// <returns>The <see cref="IApplicationBuilder" /></returns>
	IApplicationBuilder Configure(Action<IHostBuilder, Window> configureHost);

	/// <summary>
	/// Invokes any supplied delegates passed to the Configure method
	/// and then calls the internal Build on the <see cref="IHostBuilder" />
	/// </summary>
	/// <returns>The <see cref="IHost" /></returns>
	[RequiresDynamicCode(UnoHost.RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(UnoHost.RequiresUnreferencedCodeMessage)]
	IHost Build();
}
