//-:cnd:noEmit
using System.Threading;
using System.Threading.Tasks;
using MyExtensionsApp.Models;

namespace MyExtensionsApp.Services;

public interface IProductService
{
	ValueTask<Product[]> GetProducts(string? term, CancellationToken ct);

	ValueTask<Review[]> GetReviews(int productId, CancellationToken ct);
}
