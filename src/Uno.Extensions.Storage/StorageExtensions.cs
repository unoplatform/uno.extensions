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
}
