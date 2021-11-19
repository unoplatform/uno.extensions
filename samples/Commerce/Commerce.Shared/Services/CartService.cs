using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;

namespace Commerce.Services
{
	class CartService : JsonDataService<Product>, ICartService
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

	record CartItem(Product Product, int Quantity) { }

	record Cart(CartItem[] Items) {
		public string SubTotal => "$350,97";
		public string Tax => "$15,75";
		public string Total => "$405,29";
	}

	interface ICartService
	{
		ValueTask<Cart> Get(CancellationToken ct);
	}
}
