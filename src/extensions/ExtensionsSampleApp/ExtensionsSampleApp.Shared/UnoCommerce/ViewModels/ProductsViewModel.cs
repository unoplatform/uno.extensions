using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using ExtensionsSampleApp.UnoCommerce.Services;
using Uno.Extensions;

namespace ExtensionsSampleApp.UnoCommerce.ViewModels;

public class ProductsViewModel : ObservableObject
{
    private string _filterQuery;

    public string FilterQuery { get => _filterQuery; set => SetProperty(ref _filterQuery,value); }
    public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();

    public ProductsViewModel(IProductService products)
    {
        var productItems = products.GetProducts();
        productItems.ForEach(p => Products.Add(p));

        FilterQuery = "Query-" + DateTime.Now.ToString("HH:mm:ss:ffff");
    }

}


