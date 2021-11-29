using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;

namespace Commerce.Services;

public interface IProductService
{
	ValueTask<Product[]> GetProducts(string? term, CancellationToken ct);

	ValueTask<Review[]> GetReviews(int productId, CancellationToken ct);
}
