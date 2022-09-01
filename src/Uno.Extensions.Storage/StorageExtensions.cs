namespace Uno.Extensions.Storage;

public static class StorageExtensions
{
	public static async Task<TData?> ReadFileAsync<TData>(this IStorage storage, ISerializer serializer, string fileName)
	{
		using var stream = await storage.OpenFileAsync(fileName);
		return serializer.FromStream<TData>(stream);
	}
}
