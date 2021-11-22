using System.Threading.Tasks;
using Uno.Extensions.Storage;

namespace Uno.Extensions.Serialization;

public class JsonDataService<TData> : IJsonDataService<TData>
{
	public string? DataFile { get; set; }

	private TData[]? Entities { get; set; }

	private IStreamSerializer Serializer { get; }

	private IStorageProxy Storage { get; }

	public JsonDataService(IStreamSerializer serializer, IStorageProxy storage)
	{
		Serializer = serializer;
		Storage = storage;
	}

	private async Task Load()
	{
		if (Entities is not null ||
			DataFile is null)
		{
			return;
		}

		Entities = await Serializer.ReadFromFile<TData[]>(Storage, DataFile);
	}

	public async Task<TData[]?> GetEntities()
	{
		await Load();
		return Entities;
	}
}
