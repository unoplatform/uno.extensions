using System;
using System.Linq;
using Commerce.Data.Models;

namespace Commerce.Models;

public record CartItem(Product Product, uint Quantity)
{
	public CartItem(CartItemData data) :this(new Product(data.Product, false /*TODO*/), data.Quantity)
	{
	}
}
