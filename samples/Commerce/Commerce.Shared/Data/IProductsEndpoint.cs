using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Data.Models;

namespace Commerce.Data;

public interface IProductsEndpoint
{
	ValueTask<ProductData[]> GetAll(CancellationToken ct);

	ValueTask<ReviewData[]> GetReviews(int productId, CancellationToken ct);
}
