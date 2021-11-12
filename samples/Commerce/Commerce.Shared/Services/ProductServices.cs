using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
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

public class ProductService : JsonDataService<Product>, IProductService
{
	public ProductService(string dataFile) : base(dataFile)
	{

	}

	public async Task<IEnumerable<Product>> GetProducts(string? term, CancellationToken ct)
	{
		await Task.Delay(new Random(DateTime.Now.Millisecond).Next(100, 1000), ct);

		var products = (await GetEntities()).AsEnumerable();
		if (term is not null)
		{
			products = products.Where(p => p.Name.Contains(term));
		}

		return products;
	}
}

public interface IProductService
{
	Task<IEnumerable<Product>> GetProducts(string? term, CancellationToken ct);
}

public class Product
{
	public int ProductId { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string Category { get; set; }
	public string FullPrice { get; set; }
	public string Price { get; set; }
	public string Discount { get; set; }
	public string Photo { get; set; }

	public Review[] Reviews { get; set; }

}

public class Review
{
	public string Photo { get; set; }
	public string Name { get; set; }
	public string Message { get; set; }
}
