using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;
using Commerce.Services;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

[ReactiveBindable(true)]
public partial class CartProductDetailsViewModel: ProductDetailsViewModel
{
	private readonly CartItem _cartItem;

	public CartProductDetailsViewModel(
		IProductService productService,
		CartItem cartItem) : base(productService,cartItem.Product)
	{
		_cartItem = cartItem;
	}

	public override IFeed<Product> Product => base.Product;

	public override IFeed<IImmutableList<Review>> Reviews => base.Reviews;
}
