namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public record ChefsCategoryWithCount
{
	internal ChefsCategoryWithCount(int count, ChefsCategory category)
	{
		Count = count;
		Category = category;
	}

	public int Count { get; init; }
	public ChefsCategory Category { get; init; }
}
