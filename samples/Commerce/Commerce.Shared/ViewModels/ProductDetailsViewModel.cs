using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Commerce.Services;
using System.Threading.Tasks;

namespace Commerce.ViewModels;

public class ProductDetailsViewModel : ObservableObject
{
    private Product _product;

    public Product Product { get => _product; set => SetProperty(ref _product, value); }

    public ProductDetailsViewModel(IProductService products, Product p, IDictionary<string, object> parameters)
    {
		Load(products, p, parameters);
    }

	private async Task Load(IProductService products, Product p, IDictionary<string, object> parameters)
	{

		if (p is not null)
		{
			Product = p;
		}
		else
		{
			if (parameters.TryGetValue("ProductId", out var id))
			{
				Product = (await products.GetProducts()).FirstOrDefault(x => x.ProductId + "" == id.ToString());
			}
		}
	}
}
