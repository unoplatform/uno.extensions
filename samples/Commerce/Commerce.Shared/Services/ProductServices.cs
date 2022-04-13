using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;
using Uno.Extensions.Serialization;
using Uno.Extensions.Storage;

namespace Commerce.Services;

public class ProductService : IProductService
{
	public const string ProductDataFile = "products.json";
	private const string ReviewDataFile = "reviews.json";

	private readonly IStorage _dataService;
	private readonly IStreamSerializer _streamSerializer;

	public ProductService(IStorage dataService, IStreamSerializer streamSerializer)
	{
		_dataService = dataService;
		_streamSerializer = streamSerializer;
	}

	public async ValueTask<Product[]> GetProducts(string? term, CancellationToken ct)
	{
		var entities = await _dataService.GetDataAsync<Product[]>(_streamSerializer, ProductService.ProductDataFile);
		var products = entities!.AsEnumerable();
		if (term is not null)
		{
			products = products.Where(p => p.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) != -1);
		}

		return products.ToArray();
	}

	public async ValueTask<Review[]> GetReviews(int productId, CancellationToken ct)
	{
		var reviews = await _dataService.GetDataAsync<Review[]>(_streamSerializer, ProductService.ReviewDataFile);
		return reviews!;
	}
}
