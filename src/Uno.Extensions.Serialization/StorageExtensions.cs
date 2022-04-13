namespace Uno.Extensions.Serialization;

public static class StorageExtensions
{
	public static async Task<TData?> GetDataAsync<TData>(this IStorage storage, IStreamSerializer serializer, string fileName)
	{
		return await serializer.ReadFromFile<TData>(storage, fileName);
	}
}
