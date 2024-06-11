namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public partial record ChefsCategory
{
	//internal ChefsCategory(CategoryData? category)
	//{
	//	Id = category?.Id;
	//	UrlIcon = category?.UrlIcon;
	//	Name = category?.Name;
	//	Color = category?.Color;
	//}

	public int? Id { get; init; }
	public string? UrlIcon { get; init; }
	public string? Name { get; init; }
	public string? Color { get; init; }

	//internal CategoryData ToData() => new()
	//{
	//	Id = Id,
	//	UrlIcon = UrlIcon,
	//	Name = Name
	//};
}
