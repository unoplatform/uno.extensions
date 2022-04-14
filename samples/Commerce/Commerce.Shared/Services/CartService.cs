using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;
using Uno.Extensions.Serialization;
using Uno.Extensions.Storage;

namespace Commerce.Services;

class CartService : ICartService
{
	private readonly IStorage _dataService;
	private readonly ISerializer _serializer;

	public CartService(IStorage dataService, ISerializer serializer)
	{
		_dataService = dataService;
		_serializer = serializer;
	}

	public async ValueTask<Cart> Get(CancellationToken ct)
	{
		var entities = await _dataService.ReadFileAsync<Product[]>(_serializer, ProductService.ProductDataFile);
		var cart = new Cart(entities!.Select(e => new CartItem(e, 1)).ToArray());
		return cart;
	}

}
