namespace Commerce.Data;

public interface IProductsEndpoint
{
	ValueTask<ProductData[]> GetAll(CancellationToken ct);

	ValueTask<ReviewData[]> GetReviews(int productId, CancellationToken ct);
}
