using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using ExtensionsSampleApp.UnoCommerce.Services;

namespace ExtensionsSampleApp.UnoCommerce.ViewModels;

public class ProductDetailsViewModel : ObservableObject
{
    private Product _product;

    public Product Product { get => _product; set => SetProperty(ref _product, value); }

    public ProductDetailsViewModel(Product p)
    {
        Product = p;
    }
}
