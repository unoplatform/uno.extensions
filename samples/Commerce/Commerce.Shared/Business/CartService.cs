using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Data;
using Commerce.Models;
using Uno.Extensions.Reactive;
using Uno.Extensions.Serialization;
using Uno.Extensions.Storage;

namespace Commerce.Services;

class CartService : ICartService
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
