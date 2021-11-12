using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Commerce.Services;
using Uno.Extensions;

namespace Commerce.ViewModels;

public class DealsViewModel
{

    public ObservableCollection<Product> HotDeals { get; } = new ObservableCollection<Product>();
    public ObservableCollection<Product> SuperDeals { get; } = new ObservableCollection<Product>();

    public DealsViewModel(IProductService products)
    {
		Load(products);
    }

	private async Task Load(IProductService products)
	{
		var productItems = await products.GetProducts();
		productItems.ForEach(p =>
		{
			HotDeals.Add(p);
			SuperDeals.Add(p);
		}
		);
	}
}
