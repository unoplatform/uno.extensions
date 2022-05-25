using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Data;
using Commerce.Models;
using Uno.Extensions;
using Uno.Extensions.Reactive;
using Uno.Extensions.Serialization;
using Uno.Extensions.Storage;

namespace Commerce.Services;

public class ProductService : IProductService
{
	private readonly IProductsEndpoint _client;

	private readonly IState<IImmutableList<Product>> _favorites;

	public ProductService(IProductsEndpoint client)
	{
		_client = client;
		_favorites = State<IImmutableList<Product>>.Empty(this);
	}

	/// <inheritdoc />
	public async ValueTask<IImmutableList<Product>> GetAll(CancellationToken ct)
	{
		var products = await _client.GetAll(ct);
		var favorites = (await _favorites)?.Select(product => product.ProductId).ToImmutableHashSet() ?? ImmutableHashSet<int>.Empty;

		return products.Select(data => new Product(data, favorites.Contains(data.ProductId))).ToImmutableList();
	}

	/// <inheritdoc />
	public async ValueTask<IImmutableList<Product>> Search(string term, CancellationToken ct)
	{
		var products = (await _client.GetAll(ct)).AsEnumerable();
		if (term is { Length: > 0 })
		{
			products = products?.Where(p => p.Name?.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		var favorites = (await _favorites)?.Select(product => product.ProductId).ToImmutableHashSet() ?? ImmutableHashSet<int>.Empty;

		return products.Select(data => new Product(data, favorites.Contains(data.ProductId))).ToImmutableList();
	}

	/// <inheritdoc />
	public async ValueTask<IImmutableList<Review>> GetReviews(int productId, CancellationToken ct)
		=> (await _client.GetReviews(productId, ct)).Select(data => new Review(data)).ToImmutableList();

	/// <inheritdoc />
	public async ValueTask<IImmutableList<Product>> GetFavorites(CancellationToken ct)
		=> await _favorites;

	/// <inheritdoc />
	public async ValueTask Update(Product product, CancellationToken ct)
	{
		// TODO
	}
}
