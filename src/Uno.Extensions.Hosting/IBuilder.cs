namespace Uno.Extensions.Hosting;

/// <summary>
/// Implemented by classes that are builders for a specific feature.
/// </summary>
public interface IBuilder
{
	/// <summary>
	/// Gets or sets the <see cref="IHostBuilder"/> instance that is used.
	/// </summary>
	IHostBuilder? HostBuilder { get; init; }
}

/// <summary>
/// Implemented by classes that are builders for a specific feature
/// and contain a settings object to be used during initialization.
/// </summary>
/// <typeparam name="T">
/// The settings type used by derived builder classes to
/// initialize a feature.
/// </typeparam>
public interface IBuilder<T> : IBuilder
	where T : new()
{
	/// <summary>
	/// Gets or sets the settings object used by derived builder classes to
	/// initialize a feature.
	/// </summary>
	T Settings { get; set; }
}
