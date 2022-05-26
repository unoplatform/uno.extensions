namespace Commerce.Data;

internal class CartEndpoint : ICartEndpoint
{
	private readonly IStorage _dataService;
	private readonly ISerializer _serializer;

	public CartEndpoint(IStorage dataService, ISerializer serializer)
	{
		_dataService = dataService;
		_serializer = serializer;
	}

	public async ValueTask<CartData> Get(CancellationToken ct)
	{
		var products = await _dataService.ReadFileAsync<ProductData[]>(_serializer, ProductsEndpoint.ProductDataFile);
		var cart = new CartData(products?.Select(product => new CartItemData(product, 1)).ToImmutableList() ?? ImmutableList<CartItemData>.Empty);

		return cart;
	}
}
