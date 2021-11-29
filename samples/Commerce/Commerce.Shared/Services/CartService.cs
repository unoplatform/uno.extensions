using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;

namespace Commerce.Services;

public class CartService : JsonDataService<Product>, ICartService
{
	public CartService(string dataFile) : base(dataFile)
	{

	}

	public async ValueTask<Cart> Get(CancellationToken ct)
	{
		var entities = await GetEntities();
		var cart = new Cart(entities.Select(e => new CartItem(e, 1)).ToArray());
		return cart;
	}

}
