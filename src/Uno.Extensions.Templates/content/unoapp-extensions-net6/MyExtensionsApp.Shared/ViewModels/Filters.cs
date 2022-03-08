//-:cnd:noEmit
using System;
using System.Linq;
using MyExtensionsApp.Models;
using MyExtensionsApp.Services;

namespace MyExtensionsApp.ViewModels;

public record Filters
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

		if (Shoes && product.Category.IndexOf("Shoes", StringComparison.OrdinalIgnoreCase) != - 1)
		{
			return true;
		}

		if (Accessories && product.Category.IndexOf("Accessories", StringComparison.OrdinalIgnoreCase) != -1)
		{
			return true;
		}

		if (Headwear && product.Category.IndexOf("Headwear", StringComparison.OrdinalIgnoreCase) != -1)
		{
			return true;
		}

		return false;
	}
}
