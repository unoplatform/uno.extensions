using System;
using System.Collections.Generic;
using System.Text;

namespace Commerce.Services;

public class ProductService : IProductService
{
    public IEnumerable<Product> GetProducts()
    {
        return new List<Product>
        {
           new Product{ProductId=1, Name="ProMaster headphones", Category="Technology", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto1.png"},
            new Product{ProductId=2, Name="Ray-gen sunglasses", Category="Accessories", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto2.png"},
             new Product{
                ProductId=3,
                Name="Jeffords sneakers",
                Category="Men's shoes",
                FullPrice="$130",
                Price="$99",
                Discount="Save 25%",
                Photo="ms-appx:///Assets/Photos/stockphoto3.png",
                Description="The classic low top silhouette is reinvented with a water-resistant feature. The perfect go-to pair to sport on light rainy days!",
                Reviews= new[]{
                    new Review { Name = "Jean-Ralphio", Message = "Really good shoes. Love them" },
                    new Review{Name="Eric", Message="Instant buy, instant classic"},
                    new Review{Name="Lisa Kudrow", Message="I feel like walking on clouds with these shoes. Never experienced somthing simliar"}
                }
            },
            new Product{ProductId=4, Name="Wheel watch 2019", Category="Watches", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto4.png"},
            new Product{ProductId=1, Name="ProMaster headphones", Category="Technology",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto1.png"},
            new Product{ProductId=2, Name="Ray-gen sunglasses", Category="Accessories", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto2.png"},
            new Product{ProductId=3, Name="Jeffords sneakers", Category="Men's shoes",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto3.png"},
            new Product{ProductId=4, Name="Wheel watch 2019", Category="Watches", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto4.png"},
            new Product{ProductId=1, Name="ProMaster headphones", Category="Technology",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto1.png"},
            new Product{ProductId=2, Name="Ray-gen sunglasses", Category="Accessories", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto2.png"},
            new Product{ProductId=3, Name="Jeffords sneakers", Category="Men's shoes",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto3.png"},
            new Product{ProductId=4, Name="Wheel watch 2019", Category="Watches", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto4.png"},
            new Product{ProductId=1, Name="ProMaster headphones", Category="Technology",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto1.png"},
            new Product{ProductId=2, Name="Ray-gen sunglasses", Category="Accessories", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto2.png"},
            new Product{ProductId=3, Name="Jeffords sneakers", Category="Men's shoes",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto3.png"},
            new Product{ProductId=4, Name="Wheel watch 2019", Category="Watches", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto4.png"},
            new Product{ProductId=1, Name="ProMaster headphones", Category="Technology",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto1.png"},
            new Product{ProductId=2, Name="Ray-gen sunglasses", Category="Accessories", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto2.png"},
            new Product{
                ProductId=3,
                Name="Jeffords sneakers",
                Category="Men's shoes",
                FullPrice="$130",
                Price="$99",
                Discount="Save 25%",
                Photo="ms-appx:///Assets/Photos/stockphoto3.png",
                Description="The classic low top silhouette is reinvented with a water-resistant feature. The perfect go-to pair to sport on light rainy days!",
                Reviews= new[]{
                    new Review { Name = "Jean-Ralphio", Message = "Really good shoes. Love them" },
                    new Review{Name="Eric", Message="Instant buy, instant classic"},
                    new Review{Name="Lisa Kudrow", Message="I feel like walking on clouds with these shoes. Never experienced somthing simliar"}
                }
            },
            new Product{ProductId=4, Name="Wheel watch 2019", Category="Watches", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto4.png"},

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
    public string Description { get; set; }
    public string Category { get; set; }
    public string FullPrice { get; set; }
    public string Price { get; set; }
    public string Discount { get; set; }
    public string Photo { get; set; }

    public Review[] Reviews { get; set; }

}

public class Review
{
    public string Photo { get; set; }
    public string Name { get; set; }
    public string Message { get; set; }
}
