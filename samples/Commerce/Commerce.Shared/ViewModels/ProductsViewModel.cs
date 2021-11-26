using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;
using Commerce.Services;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

public partial class ProductsViewModel
{
	private readonly IProductService _products;
	private readonly IFeed<string> _term;
	private readonly IFeed<Filters> _filter;

	private ProductsViewModel(
		IProductService products,
		IInput<string> term,
		[Value] IInput<Filters> filter)
	{
		_products = products;
		_term = term;
		_filter = filter;
	}

	public IFeed<Product[]> Items => Feed
		.Combine(_term.SelectAsync(Load), _filter)
		.Select(FilterProducts)
		.Where(products => products.Any());

	private async ValueTask<Product[]> Load(string searchTerm, CancellationToken ct)
	{
		var products = await _products.GetProducts(searchTerm, ct);

		return products.ToArray();
	}

	private Product[] FilterProducts((Product[] products, Filters? filter) inputs)
	{
		return inputs.products.Where(p => inputs.filter?.Match(p) ?? true).ToArray();
	}
}
