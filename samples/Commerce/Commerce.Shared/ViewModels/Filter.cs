using System;
using System.Linq;
using Commerce.Services;

namespace Commerce.ViewModels;

public record Filter
{
	public bool Shoes { get; set; }

	public bool Accessories { get; set; }

	public bool Headwear { get; set; }

	public bool Match(Product product)
	{
		if (!Shoes && !Accessories && !Headwear)
		{
			return true;
		}

		if (Shoes && product.Category.Contains("Shoes", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (Accessories && product.Category.Contains("Accessories", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (Headwear && product.Category.Contains("Headwear", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return false;
	}
}
