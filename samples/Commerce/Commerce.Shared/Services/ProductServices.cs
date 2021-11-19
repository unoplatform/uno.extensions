using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;

namespace Commerce.Services;

public class ProductService : JsonDataService<Product>, IProductService
{
	public ProductService(string dataFile) : base(dataFile)
	{
	}

	public async Task<IEnumerable<Product>> GetProducts(string? term, CancellationToken ct)
	{
		var products = (await GetEntities()).AsEnumerable();
		if (term is not null)
		{
			products = products.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
		}

		return products;
	}
}
