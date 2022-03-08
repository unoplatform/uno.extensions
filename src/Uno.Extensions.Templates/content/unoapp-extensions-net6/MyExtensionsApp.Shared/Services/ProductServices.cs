//-:cnd:noEmit
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyExtensionsApp.Models;
using Uno.Extensions.Serialization;

namespace MyExtensionsApp.Services;

public class ProductService : IProductService
{
	public const string ProductDataFile = "products.json";
	private const string ReviewDataFile = "reviews.json";

	private readonly IJsonDataService<Product> _productDataService;
	private readonly IJsonDataService<Review> _reviewDataService;
	public ProductService(IJsonDataService<Product> products, IJsonDataService<Review> reviews) 
	{
		_productDataService = products;
		_productDataService.DataFile= ProductDataFile;

		_reviewDataService = reviews;
		_reviewDataService.DataFile= ReviewDataFile;
	}

	public async ValueTask<Product[]> GetProducts(string? term, CancellationToken ct)
	{
		var products = (await _productDataService.GetEntities()).AsEnumerable();
		if (term is not null)
		{
			products = products.Where(p => p.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) != -1);
		}

		return products.ToArray();
	}

	public async ValueTask<Review[]> GetReviews(int productId, CancellationToken ct)
	{
		var reviews = await _reviewDataService.GetEntities();
		return reviews;
	}
}
