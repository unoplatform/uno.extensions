using System;
using System.Collections.Generic;
using System.Text;

namespace ExtensionsSampleApp.UnoCommerce.Services;

public class ProductService : IProductService
{
    public IEnumerable<Product> GetProducts()
    {
        return new List<Product>
        {
            new Product{ProductId = 1, Name="Wine"},
            new Product{ProductId = 2, Name="Wine"},
            new Product{ProductId = 3, Name="Wine"},
            new Product{ProductId = 4, Name="Wine"},
            new Product{ProductId = 5, Name="Wine"},
            new Product{ProductId = 6, Name="Wine"},
            new Product{ProductId = 7, Name="Wine"},
            new Product{ProductId = 8, Name="Wine"},
            new Product{ProductId = 9, Name="Wine"},
            new Product{ProductId = 10, Name="Wine"},
            new Product{ProductId = 11, Name="Wine"},
            new Product{ProductId = 12, Name="Wine"},
            new Product{ProductId = 13, Name="Wine"},
            new Product{ProductId = 14, Name="Wine"},
            new Product{ProductId = 15, Name="Wine"},
            new Product{ProductId = 16, Name="Wine"},
            new Product{ProductId = 17, Name="Wine"},
            new Product{ProductId = 18, Name="Wine"},
            new Product{ProductId = 19, Name="Wine"},
            new Product{ProductId = 20, Name="Wine"},
            new Product{ProductId = 21, Name="Wine"},
        };
    }
}

public interface IProductService
{
    IEnumerable<Product> GetProducts();
}

public class Product
{
    public int ProductId { get; set; }
    public  string Name { get; set; }

}
