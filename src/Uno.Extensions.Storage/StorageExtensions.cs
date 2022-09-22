namespace Uno.Extensions.Storage;

public static class StorageExtensions
{
	public static async Task<TData?> ReadPackageFileAsync<TData>(this IStorage storage, ISerializer serializer, string fileName)
	{
		using var stream = await storage.OpenPackageFileAsync(fileName);
		if(stream is null)
		{
			return default;
		}
		return serializer.FromStream<TData>(stream);
	}
}
