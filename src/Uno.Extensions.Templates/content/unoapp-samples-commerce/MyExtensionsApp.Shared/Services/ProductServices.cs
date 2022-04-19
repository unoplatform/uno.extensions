//-:cnd:noEmit
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyExtensionsApp.Models;
using Uno.Extensions.Serialization;
using Uno.Extensions.Storage;

namespace MyExtensionsApp.Services;

public class ProductService : IProductService
{
	public const string ProductDataFile = "products.json";
	private const string ReviewDataFile = "reviews.json";

	private readonly IStorage _dataService;
	private readonly ISerializer _serializer;

	public ProductService(IStorage dataService, ISerializer serializer)
	{
		_dataService = dataService;
		_serializer = serializer;
	}

	public async ValueTask<Product[]> GetProducts(string? term, CancellationToken ct)
	{
		var entities = await _dataService.ReadFileAsync<Product[]>(_serializer, ProductService.ProductDataFile);
		var products = entities!.AsEnumerable();
		if (term is not null)
		{
			products = products.Where(p => p.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) != -1);
		}

		return products.ToArray();
	}

	public async ValueTask<Review[]> GetReviews(int productId, CancellationToken ct)
	{
		var reviews = await _dataService.ReadFileAsync<Review[]>(_serializer, ProductService.ReviewDataFile);
		return reviews!;
	}
}
