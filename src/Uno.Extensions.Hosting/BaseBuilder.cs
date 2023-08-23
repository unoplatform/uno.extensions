namespace Uno.Extensions.Hosting;

/// <summary>
/// A base class for types that build a specific feature.
/// </summary>
public record BaseBuilder : IBuilder
{
	/// <inheritdoc/>
	public IHostBuilder? HostBuilder { get; init; }
}

/// <summary>
/// A base class for types that use a settings object to build a specific feature.
/// </summary>
/// <typeparam name="T">
/// The settings type used by derived builder classes to initialize a feature.
/// </typeparam>
public record BaseBuilder<T> : BaseBuilder, IBuilder<T>
	where T : new()
{
	/// <inheritdoc/>
	public T Settings { get; set; } = new T();
}
