using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using ExtensionsSampleApp.UnoCommerce.Services;
using Uno.Extensions;

namespace ExtensionsSampleApp.UnoCommerce.ViewModels;

public class ProductsViewModel
{
    public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();

    public ProductsViewModel(IProductService products)
    {
        var productItems = products.GetProducts();
        productItems.ForEach(p => Products.Add(p));
    }

}

public class ProductDetailsViewModel : ObservableObject
{
    private Product _product;

    public Product Product { get => _product; set => SetProperty(ref _product, value); }

    public ProductDetailsViewModel(Product p)
    {
        Product = p;
    }
}
