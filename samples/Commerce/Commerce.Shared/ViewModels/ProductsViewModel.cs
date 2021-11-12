using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Services;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

public partial class ProductsViewModel
{
	private readonly IProductService _products;
	private readonly IFeed<string> _searchTerm;
	private readonly IState<string> _filterQuery;

	private ProductsViewModel(
		IProductService products,
		[DefaultValue("")] IFeed<string> searchTerm,
		[DefaultValue("")] IState<string> filterQuery)
	{
		_products = products;
		_searchTerm = searchTerm;
		_filterQuery = filterQuery;
	}

	public IFeed<Product[]> Items => Feed
		.Combine(_searchTerm.SelectAsync(Load), _filterQuery)
		.Select(FilterProducts);

	private async ValueTask<Product[]> Load(string searchTerm, CancellationToken ct)
	{
		var products = await _products.GetProducts(searchTerm, ct);

		return products.ToArray();
	}

	private Product[] FilterProducts((Product[] products, string filterQuery) inputs)
	{
		// TODO: Apply filter here
		return inputs.products;
	}
}
