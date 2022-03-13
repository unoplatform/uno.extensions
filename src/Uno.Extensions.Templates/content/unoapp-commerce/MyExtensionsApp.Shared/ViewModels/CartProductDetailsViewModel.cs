//-:cnd:noEmit
using System.Threading;
using System.Threading.Tasks;
using MyExtensionsApp.Models;
using MyExtensionsApp.Services;
using Uno.Extensions.Reactive;

namespace MyExtensionsApp.ViewModels;

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

	public override IFeed<Review[]> Reviews => base.Reviews;
}
