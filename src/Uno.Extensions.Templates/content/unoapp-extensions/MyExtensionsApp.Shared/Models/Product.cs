namespace MyExtensionsApp.Models;

public record Product
{
	public int ProductId { get; set; }
	public string Brand { get; set; }
	public string Name { get; set; }
	public string LongName { get; set; }
	public string Description { get; set; }
	public string Category { get; set; }
	public string FullPrice { get; set; }
	public string Price { get; set; }
	public string Discount { get; set; }
	public string Photo { get; set; }
	public double? Rating { get; set; }

	public string DiscountedPrice => Price;

	public Review[] Reviews { get; set; }

}
