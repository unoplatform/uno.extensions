

namespace Commerce.Business;

public class CartService : ICartService
{
	private readonly ICartEndpoint _client;

	public CartService(ICartEndpoint client)
	{
		_client = client;
	}

	public IState<Cart> _cart => State<Cart>.Empty(this);
	public IFeed<Cart> Cart => _cart;

	public async ValueTask<Cart> Get(CancellationToken ct)
	{
		var data = await _client.Get(ct);
		var cart = new Cart(data);

		return cart;
	}
}
