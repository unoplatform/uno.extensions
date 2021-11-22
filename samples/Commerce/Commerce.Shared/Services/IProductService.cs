using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;

namespace Commerce.Services;

public interface IProductService
{
	Task<IEnumerable<Product>> GetProducts(string? term, CancellationToken? ct = default);

	Task<Review[]> GetReviews(int productId, CancellationToken? ct = default);
}
