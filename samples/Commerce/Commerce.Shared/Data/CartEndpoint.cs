using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Data.Models;
using Commerce.Models;
using Commerce.Services;
using Uno.Extensions.Serialization;
using Uno.Extensions.Storage;

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
