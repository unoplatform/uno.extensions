using System.Collections.Immutable;

namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public record ChefsLiveCookingParameter(ChefsRecipe Recipe, IImmutableList<ChefsStep> Steps);

public partial record ChefsCookbook : ChefsIChefEntity
{
	//internal ChefsCookbook(CookbookData cookbookData)
	//{
	//	Id = cookbookData.Id;
	//	UserId = cookbookData.UserId;
	//	Name = cookbookData.Name;
	//	Recipes = cookbookData.Recipes?
	//		.Select(c => new ChefsRecipe(c))
	//		.ToImmutableList();
	//	CookbookImages = new CookbookImages(cookbookData.Recipes?.ToImmutableList() ?? ImmutableList<RecipeData>.Empty);
	//}

	internal ChefsCookbook() { Recipes = ImmutableList<ChefsRecipe>.Empty; }

	public Guid Id { get; init; }
	public Guid UserId { get; init; }
	public string? Name { get; init; }
	public int PinsNumber => Recipes?.Count ?? 0;
	public IImmutableList<ChefsRecipe>? Recipes { get; init; }
	public CookbookImages? CookbookImages { get; init; }

	//internal ChefsCookbookData ToData() => new()
	//{
	//	Id = Id,
	//	UserId = UserId,
	//	Name = Name,
	//	Recipes = Recipes?
	//		.Select(c => c.ToData())
	//		.ToList()
	//};

	//internal CookbookData ToData(IImmutableList<Recipe> recipes) => new()
	//{
	//	Id = Id,
	//	UserId = UserId,
	//	Name = Name,
	//	Recipes = recipes is null
	//		? Recipes?
	//			.Select(c => c.ToData())
	//			.ToList()
	//		: recipes
	//			.Select(c => c.ToData())
	//			.ToList()
	//};

	//internal UpdateCookbook UpdateCookbook() => new(this);
}
