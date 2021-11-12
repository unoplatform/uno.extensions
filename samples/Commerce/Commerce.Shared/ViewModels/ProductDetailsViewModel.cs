using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Commerce.Services;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

public partial class ProductDetailsViewModel
{
	private readonly IProductService _products;
	private readonly Product _product;
	private readonly IDictionary<string, object> _parameters;

	public ProductDetailsViewModel(
		IProductService products,
		Product product,
		IDictionary<string, object> parameters)
	{
		_products = products;
		_product = product;
		_parameters = parameters;
	}

	public IFeed<Product> Product => Feed.Async(Load);

	private async ValueTask<Option<Product>> Load(CancellationToken ct)
	{
		await Task.Delay(5000);

		if (_product is not null)
		{
			return _product;
		}
		else if(_parameters.TryGetValue("ProductId", out var id))
		{
			return (await _products.GetProducts(null, CancellationToken.None)).FirstOrDefault(x => x.ProductId + "" == id.ToString());
		}
		else
		{
			return default;
		}
	}
}
