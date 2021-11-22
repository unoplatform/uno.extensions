using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;
using Uno.Extensions.Serialization;

namespace Commerce.Services
{
	class CartService : ICartService
	{
		private IJsonDataService<Product> _productDataService;
		public CartService(IJsonDataService<Product> products) 
		{
			_productDataService = products;
			_productDataService.DataFile = ProductService.ProductDataFile;
		}

		public async ValueTask<Cart> Get(CancellationToken ct)
		{
			var entities = await _productDataService.GetEntities();
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
