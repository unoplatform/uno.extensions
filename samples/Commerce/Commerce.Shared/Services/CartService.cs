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
	private readonly IStreamSerializer _streamSerializer;

	public CartService(IStorage dataService, IStreamSerializer streamSerializer)
	{
		_dataService = dataService;
		_streamSerializer = streamSerializer;
	}

	public async ValueTask<Cart> Get(CancellationToken ct)
	{
		var entities = await _dataService.ReadFileAsync<Product[]>(_streamSerializer, ProductService.ProductDataFile);
		var cart = new Cart(entities!.Select(e => new CartItem(e, 1)).ToArray());
		return cart;
	}

}
