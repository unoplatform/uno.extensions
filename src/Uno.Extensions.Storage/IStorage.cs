using System.Collections.Immutable;

namespace Uno.Extensions.Storage;

/// <summary>
/// Wrapper for accessing file storage for application
/// </summary>
public interface IStorage
{
	/// <summary>
	/// Create a folder relative to app data
	/// </summary>
	/// <param name="foldername">The name of the folder to create</param>
	/// <returns>Folder path is folder successfully created</returns>
	Task<string?> CreateFolderAsync(string foldername);

	/// <summary>
	/// Reads a file from the application package
	/// </summary>
	/// <param name="filename">The relative path to the file to read</param>
	/// <returns>The text contents of the file if the file can be read</returns>
	Task<string?> ReadPackageFileAsync(string filename);

	/// <summary>
	/// Reads specific line ranges from a file in the application package.
	/// </summary>
	/// <param name="filename">The relative path to the file to read.</param>
	/// <param name="lineRanges">A list of tuples specifying the start and end line numbers to select and return.</param>
	/// <returns>An <see cref="ImmutableList{T}"/> of <see langword="string"/> containing the selected lines from <paramref name="lineRanges"/>, or <see langword="null"/> if the file cannot be read.</returns>
	Task<ImmutableList<string>?> ReadPackageFileAsync(string filename, List<(int Start, int End)> lineRanges);

	/// <summary>
	/// Opens a file for reading from the application package
	/// </summary>
	/// <param name="filename">The relative path to the file to read</param>
	/// <returns>Stream for the file if it can be opened</returns>
	Task<Stream?> OpenPackageFileAsync(string filename);

	/// <summary>
	/// Writes to a file relative to app data
	/// </summary>
	/// <param name="filename">The relative path to the file to write</param>
	/// <param name="text">The text to write</param>
	/// <param name="overwrite">Whether to override existing file</param>
	/// <returns>Awaitable task</returns>
	Task WriteFileAsync(string filename, string text, bool overwrite);
}
