namespace Uno.Extensions.Hosting;

/// <summary>
/// This is a marker interface to help <see cref="IApplicationBuilder" /> Extension methods determine
/// the proper root content to use for Navigation
/// </summary>
public interface IContentControlProvider
{
	/// <summary>
	/// Returns the Loading View for Navigation
	/// </summary>
	ContentControl ContentControl { get; }
}
