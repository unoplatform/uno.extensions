using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Data.Models;
using Commerce.Models;
using Uno.Extensions.Serialization;
using Uno.Extensions.Storage;

namespace Commerce.Data;

internal class ProductsEndpoint : IProductsEndpoint
{
	public const string ProductDataFile = "products.json";
	private const string ReviewDataFile = "reviews.json";

	private readonly IStorage _dataService;
	private readonly ISerializer _serializer;

	public ProductsEndpoint(IStorage dataService, ISerializer serializer)
	{
		_dataService = dataService;
		_serializer = serializer;
	}

	public async ValueTask<ProductData[]> GetAll(CancellationToken ct)
	{
		var products = await _dataService.ReadFileAsync<IEnumerable<ProductData>>(_serializer, ProductDataFile);

		return products?.ToArray() ?? Array.Empty<ProductData>();
	}

	public async ValueTask<ReviewData[]> GetReviews(int productId, CancellationToken ct)
	{
		var reviews = await _dataService.ReadFileAsync<ReviewData[]>(_serializer, ReviewDataFile);

		return reviews ?? Array.Empty<ReviewData>();
	}
}
