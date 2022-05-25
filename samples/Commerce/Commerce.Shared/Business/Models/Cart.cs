using System;
using System.Collections.Immutable;
using System.Linq;
using Commerce.Data.Models;
using Commerce.Services;

namespace Commerce.Models;

public record Cart
{
	public Cart(CartData data)
	{
		Items = data.Items.Select(item => new CartItem(item)).ToImmutableList();
	}

	public string SubTotal => "$350,97";
	public string Tax => "$15,75";
	public string Total => "$405,29";

	public IImmutableList<CartItem> Items { get; init; }

	public Cart Add(Product product)
	{
		var item = Items.FirstOrDefault(item => item.Product.ProductId == product.ProductId);
		var updatedItems = item is null
			? Items.Add(new CartItem(product, 1))
			: Items.Replace(item, item with { Quantity = item.Quantity + 1 });

		return this with { Items = updatedItems };
	}

	public Cart Update(Product product, uint quantity)
	{
		var item = Items.FirstOrDefault(item => item.Product.ProductId == product.ProductId);
		if (item is null && quantity is 0)
		{
			return this;
		}

		var updatedItems = item is null
			? Items.Add(new CartItem(product, quantity))
			: Items.Replace(item, item with { Quantity = quantity });

		return this with { Items = updatedItems };
	}

	public Cart Remove(Product product)
		=> Update(product, 0);
}
