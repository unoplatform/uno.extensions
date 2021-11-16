using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;


namespace Commerce.Services;

public class JsonDataService<TData>
{
	private string DataFile { get; }

	private TData[] Entities { get; set; }

	public JsonDataService(string dataFile)
	{
		DataFile = dataFile;
	}

	private async Task Load()
	{
		if (Entities is not null)
		{
			return;
		}

		var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{DataFile}"));
		using var stream = await storageFile.OpenStreamForReadAsync();

		Entities = JsonSerializer.Deserialize<TData[]>(stream, new JsonSerializerOptions
		{
			NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
			AllowTrailingCommas = true
		});
	}

	public async Task<TData[]> GetEntities()
	{
		await Load();
		return Entities;
	}
}
