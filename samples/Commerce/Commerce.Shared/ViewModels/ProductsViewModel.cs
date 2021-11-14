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
	private readonly IFeed<Filter> _filter;

	private ProductsViewModel(
		IProductService products,
		IFeed<string> searchTerm,
		[Edit] IFeed<Filter> filter)
	{
		_products = products;
		_searchTerm = searchTerm;
		_filter = filter;
	}

	public IFeed<Product[]> Items => Feed
		.Combine(_searchTerm.SelectAsync(Load), _filter)
		.Select(FilterProducts);

	private async ValueTask<Product[]> Load(string searchTerm, CancellationToken ct)
	{
		var products = await _products.GetProducts(searchTerm, ct);

		return products.ToArray();
	}

	private Product[] FilterProducts((Product[] products, Filter? filter) inputs)
	{
		return inputs.products.Where(p => inputs.filter?.Match(p) ?? true).ToArray();
	}
}
