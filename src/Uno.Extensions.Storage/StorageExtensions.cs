namespace Uno.Extensions.Storage;

/// <summary>
/// Extensions for working with <see cref="IStorage"/>.
/// </summary>
public static class StorageExtensions
{
	/// <summary>
	/// Reads the contents of a file and deserializes to the specified type
	/// </summary>
	/// <typeparam name="TData">The type to deserialize to</typeparam>
	/// <param name="storage">The storage instance</param>
	/// <param name="serializer">The serializer to use</param>
	/// <param name="fileName">The relative path of the file to read from</param>
	/// <returns>The instance read, or null if file isn't found </returns>
	public static async Task<TData?> ReadPackageFileAsync<TData>(this IStorage storage, ISerializer serializer, string fileName)
	{
		using var stream = await storage.OpenPackageFileAsync(fileName);
		if (stream is null)
		{
			return default;
		}
		return serializer.FromStream<TData>(stream);
	}

	/// <summary>
	/// Reads specific lines from a file asynchronously based on the provided line ranges.
	/// </summary>
	/// <param name="storage">The storage interface used to access the file.</param>
	/// <param name="filePath">The path of the file to read from.</param>
	/// <param name="lineRanges">
	/// A collection of tuples representing the line ranges to extract. 
	/// Each tuple contains a start line (inclusive) and an end line (inclusive).
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation. 
	/// The result contains the extracted lines joined by the system's newline character, 
	/// or the entire file content if no line ranges are specified.
	/// </returns>
	public static async ValueTask<string> ReadLinesFromPackageFile(this IStorage storage, string filePath, IEnumerable<(int, int)> lineRanges)
	{
		string fileContent = await storage.ReadPackageFileAsync(filePath) ?? string.Empty;

		if (!(lineRanges.Any()))
		{
			return fileContent;
		}

		return fileContent.Split(Environment.NewLine)
						  .SelectItemsByRanges(lineRanges, isNullBased: false)
						  .JoinBy(Environment.NewLine);
	}
}
