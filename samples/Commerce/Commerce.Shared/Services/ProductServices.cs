using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace Commerce.Services;

public class JsonDataService<TData>
{
	private string DataFile { get; }

	private TData[] Entities { get; set; }

	public JsonDataService(string dataFile)
	{
		DataFile = dataFile;
	}

	private async Task Load()
	{
		if (Entities is not null)
		{
			return;
		}

		var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{DataFile}"));
		using var stream = await storageFile.OpenStreamForReadAsync();

		Entities = JsonSerializer.Deserialize<TData[]>(stream, new JsonSerializerOptions
		{
			NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
			AllowTrailingCommas = true
		});
	}

	public async Task<TData[]> GetEntities()
	{
		await Load();
		return Entities;
	}
}

public class ProductService : JsonDataService<Product>, IProductService
{
	public ProductService(string dataFile) : base(dataFile)
	{

	}

	public Task<Product[]> GetProducts()
	{
		return base.GetEntities();
		//return new List<Product>
		//{
		//   new Product{ProductId=1, Name="ProMaster headphones", Category="Technology", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto1.png"},
		//	new Product{ProductId=2, Name="Ray-gen sunglasses", Category="Accessories", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto2.png"},
		//	 new Product{
		//		ProductId=3,
		//		Name="Jeffords sneakers",
		//		Category="Men's shoes",
		//		FullPrice="$130",
		//		Price="$99",
		//		Discount="Save 25%",
		//		Photo="ms-appx:///Assets/Photos/stockphoto3.png",
		//		Description="The classic low top silhouette is reinvented with a water-resistant feature. The perfect go-to pair to sport on light rainy days!",
		//		Reviews= new[]{
		//			new Review { Name = "Jean-Ralphio", Message = "Really good shoes. Love them" },
		//			new Review{Name="Eric", Message="Instant buy, instant classic"},
		//			new Review{Name="Lisa Kudrow", Message="I feel like walking on clouds with these shoes. Never experienced somthing simliar"}
		//		}
		//	},
		//	new Product{ProductId=4, Name="Wheel watch 2019", Category="Watches", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto4.png"},
		//	new Product{ProductId=1, Name="ProMaster headphones", Category="Technology",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto1.png"},
		//	new Product{ProductId=2, Name="Ray-gen sunglasses", Category="Accessories", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto2.png"},
		//	new Product{ProductId=3, Name="Jeffords sneakers", Category="Men's shoes",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto3.png"},
		//	new Product{ProductId=4, Name="Wheel watch 2019", Category="Watches", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto4.png"},
		//	new Product{ProductId=1, Name="ProMaster headphones", Category="Technology",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto1.png"},
		//	new Product{ProductId=2, Name="Ray-gen sunglasses", Category="Accessories", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto2.png"},
		//	new Product{ProductId=3, Name="Jeffords sneakers", Category="Men's shoes",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto3.png"},
		//	new Product{ProductId=4, Name="Wheel watch 2019", Category="Watches", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto4.png"},
		//	new Product{ProductId=1, Name="ProMaster headphones", Category="Technology",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto1.png"},
		//	new Product{ProductId=2, Name="Ray-gen sunglasses", Category="Accessories", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto2.png"},
		//	new Product{ProductId=3, Name="Jeffords sneakers", Category="Men's shoes",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto3.png"},
		//	new Product{ProductId=4, Name="Wheel watch 2019", Category="Watches", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto4.png"},
		//	new Product{ProductId=1, Name="ProMaster headphones", Category="Technology",FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto1.png"},
		//	new Product{ProductId=2, Name="Ray-gen sunglasses", Category="Accessories", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto2.png"},
		//	new Product{
		//		ProductId=3,
		//		Name="Jeffords sneakers",
		//		Category="Men's shoes",
		//		FullPrice="$130",
		//		Price="$99",
		//		Discount="Save 25%",
		//		Photo="ms-appx:///Assets/Photos/stockphoto3.png",
		//		Description="The classic low top silhouette is reinvented with a water-resistant feature. The perfect go-to pair to sport on light rainy days!",
		//		Reviews= new[]{
		//			new Review { Name = "Jean-Ralphio", Message = "Really good shoes. Love them" },
		//			new Review{Name="Eric", Message="Instant buy, instant classic"},
		//			new Review{Name="Lisa Kudrow", Message="I feel like walking on clouds with these shoes. Never experienced somthing simliar"}
		//		}
		//	},
		//	new Product{ProductId=4, Name="Wheel watch 2019", Category="Watches", FullPrice="$130",Price="$99", Discount="Save 25%", Photo="ms-appx:///Assets/Photos/stockphoto4.png"},

		//};
	}
}

public interface IProductService
{
	Task<Product[]> GetProducts();
}

public class Product
{
	public int ProductId { get; set; }
	public string Name { get; set; }
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
