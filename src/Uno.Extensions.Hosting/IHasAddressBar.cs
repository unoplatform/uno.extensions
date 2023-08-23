namespace Uno.Extensions.Hosting;

/// <summary>
/// Implemented by hosting environment classes that specifically support an address bar.
/// For instance, the AppHostingEnvironment class conditionally implements this interface on WebAssembly.
/// </summary>
public interface IHasAddressBar
{
	/// <summary>
	/// Updates the address bar with the specified URI.
	/// </summary>
	/// <param name="applicationUri">
	/// The URI to update the address bar with.
	/// </param>
	/// <returns>
	/// A task that completes when the address bar has been updated.
	/// </returns>
	Task UpdateAddressBar(Uri applicationUri);
}
