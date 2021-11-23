using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;
using Uno.Extensions;

namespace Commerce.Services
{
	class DealService : JsonDataService<Product>, IDealService
	{

		public DealService(string dataFile) : base(dataFile) { }

		public async ValueTask<Product[]> GetDeals(CancellationToken ct)
		{
			var products = await GetEntities();

			return products.Where(p => !p.Discount.IsNullOrEmpty()).ToArray();
		}
	}

	public interface IDealService
	{
		ValueTask<Product[]> GetDeals(CancellationToken ct);
	}
}
