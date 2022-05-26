namespace Commerce.ViewModels;

public partial record CartViewModel(ICartService CartService)
{
	public IFeed<Cart> Cart => CartService.Cart;

	public async ValueTask Remove(CartItem item, CancellationToken ct)
		=> await CartService.Remove(item.Product, ct);
}
