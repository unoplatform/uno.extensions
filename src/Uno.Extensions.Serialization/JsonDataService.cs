using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;


namespace Uno.Extensions.Serialization;

public class JsonDataService<TData> : IJsonDataService<TData>
{
	public string? DataFile { get; set; }

	private TData[]? Entities { get; set; }

	private IStreamSerializer Serializer { get; }

	public JsonDataService(IStreamSerializer serializer)
	{
		Serializer = serializer;
	}

	private async Task Load()
	{
		if (Entities is not null ||
			DataFile is null)
		{
			return;
		}

		Entities = await Serializer.ReadFromFile<TData[]>(DataFile);
	}

	public async Task<TData[]?> GetEntities()
	{
		await Load();
		return Entities;
	}
}
