namespace Commerce.Business;

public class ProductService : IProductService
{
	private readonly IProductsEndpoint _client;

	private ImmutableHashSet<int> _favorites = ImmutableHashSet<int>.Empty;

	public ProductService(IProductsEndpoint client)
	{
		_client = client;
	}

	/// <inheritdoc />
	public async ValueTask<IImmutableList<Product>> GetAll(CancellationToken ct)
		=> ToProduct(await _client.GetAll(ct));

	/// <inheritdoc />
	public async ValueTask<IImmutableList<Product>> Search(string term, CancellationToken ct)
	{
		var products = (await _client.GetAll(ct)).AsEnumerable();
		if (term is { Length: > 0 })
		{
			products = products.Where(p => p.Name?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		return ToProduct(products);
	}

	/// <inheritdoc />
	public async ValueTask<IImmutableList<Review>> GetReviews(int productId, CancellationToken ct)
		=> (await _client.GetReviews(productId, ct)).Select(data => new Review(data)).ToImmutableList();

	/// <inheritdoc />
	public async ValueTask<IImmutableList<Product>?> GetFavorites(CancellationToken ct)
		=> (await _client.GetAll(ct))
			.Where(product => _favorites.Contains(product.ProductId))
			.Select(product => new Product(product, isFavorite: true))
			.ToImmutableList();

	/// <inheritdoc />
	public async ValueTask Update(Product product, CancellationToken ct)
		=> ImmutableInterlocked.Update(
			ref _favorites,
			(favs, prod) => prod.IsFavorite ? favs.Add(prod.ProductId) : favs.Remove(prod.ProductId),
			product);

	private IImmutableList<Product> ToProduct(IEnumerable<ProductData> data)
		=> data
			.Select(d => new Product(d, _favorites.Contains(d.ProductId)))
			.ToImmutableList();
}
