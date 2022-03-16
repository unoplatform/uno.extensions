//-:cnd:noEmit
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyExtensionsApp.Models;
using MyExtensionsApp.Services;
using Uno.Extensions.Reactive;

namespace MyExtensionsApp.ViewModels;

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
		.Combine(Results, _filter)
		.Select(FilterProducts)
		.Where(products => products.Any());

	private IFeed<Product[]> Results => _term
		.SelectAsync(_products.GetProducts);

	private Product[] FilterProducts((Product[] products, Filters? filter) inputs)
		=> inputs.products.Where(p => inputs.filter?.Match(p) ?? true).ToArray();
}
