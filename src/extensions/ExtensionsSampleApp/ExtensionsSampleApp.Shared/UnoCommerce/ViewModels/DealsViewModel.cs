using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using ExtensionsSampleApp.UnoCommerce.Services;
using Uno.Extensions;

namespace ExtensionsSampleApp.UnoCommerce.ViewModels
{
    public class DealsViewModel
    {

        public ObservableCollection<Product> HotDeals { get; } = new ObservableCollection<Product>();
        public ObservableCollection<Product> SuperDeals { get; } = new ObservableCollection<Product>();

        public DealsViewModel(IProductService products)
        {
            var productItems = products.GetProducts();
            productItems.ForEach(p =>
            {
                HotDeals.Add(p);
                SuperDeals.Add(p);
            }
            );
        }
    }
}
