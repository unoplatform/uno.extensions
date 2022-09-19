namespace Uno.Extensions.Hosting;

/// <summary>
/// Defines services that require initialization immediately after the IHost is built
/// </summary>
public interface IServiceInitialize
{
	/// <summary>
	/// Initializes service immediately after IHost is built
	/// </summary>
	void Initialize();
}
