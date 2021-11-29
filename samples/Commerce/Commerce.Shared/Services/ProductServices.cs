using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;

namespace Commerce.Services;

public class ProductService : JsonDataService<Product>, IProductService
{
	private readonly JsonDataService<Review> _reviewDataService;
	public ProductService(string productDataFile, string reviewDataFile) : base(productDataFile)
	{
		_reviewDataService = new JsonDataService<Review>(reviewDataFile);
	}

	public async ValueTask<Product[]> GetProducts(string? term, CancellationToken ct)
	{
		var products = (await GetEntities()).AsEnumerable();
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
