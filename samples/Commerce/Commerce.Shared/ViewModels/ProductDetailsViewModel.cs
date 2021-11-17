using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;
using Commerce.Services;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

public partial class ProductDetailsViewModel
{
	private readonly IProductService _productService;
	private readonly Product _product;

	public ProductDetailsViewModel(
		IProductService productService,
		Product product)
	{
		_productService = productService;
		_product = product;
	}

	public IFeed<Product> Product => Feed.Async(Load);

	private async ValueTask<Option<Product>> Load(CancellationToken ct)
	{
		await Task.Delay(500);

		if (_product is not null)
		{
			return _product;
		}
		else
		{
			return default;
		}
	}
}
