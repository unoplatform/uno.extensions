namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public class CookbookImages
{
	//public ChefsCookbookImages(ImmutableList<RecipeData> recipesData)
	//{
	//	FirstImage = recipesData.Count > 0
	//		? recipesData[0].ImageUrl
	//		: null;
	//	SecondImage = recipesData.Count > 1
	//		? recipesData[1].ImageUrl
	//		: null;
	//	ThirdImage = recipesData.Count > 2
	//		? recipesData[2].ImageUrl
	//		: null;
	//}

	public string? FirstImage { get; set; }

	public string? SecondImage { get; set; }

	public string? ThirdImage { get; set; }
}
