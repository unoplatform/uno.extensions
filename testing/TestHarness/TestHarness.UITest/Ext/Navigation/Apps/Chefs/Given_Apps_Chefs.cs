
using Xamarin.UITest.Shared.Extensions;

namespace TestHarness.UITest;

public class Given_Apps_Chefs : NavigationTestBase
{
	private const string HomePage = "Home";
	private const string SearchPage = "Search";
	private const string FavoritesPage = "Favorites";
	private const string RecipeDetails = "RecipeDetails";
	private const string CookbookDetails = "CookbookDetails";
	private const string LiveCooking = "LiveCooking";


	[Test]
	public async Task When_Login_Home_Is_Default()
	{
		InitTestSection(TestSections.Apps_Chefs);

		App.WaitThenTap("ShowAppButton");
		App.WaitThenTap("NextButton");
		App.WaitThenTap("LoginButton");

		var tabs = App.Marked("MainTabs");

		//Make sure we default to the Home Tab (first tab)
		App.WaitForElement("HomeNavigationBar");
		App.WaitForDependencyPropertyValue(tabs, "SelectedIndex", "0");
	}

	[Test]
	public async Task When_Root_Home_Selected()
	{
		InitTestSection(TestSections.Apps_Chefs);

		await NavToRootPage(HomePage);
	}
	
	[Test]
	public async Task When_Root_Search_Selected()
	{
		InitTestSection(TestSections.Apps_Chefs);

		await NavToRootPage(SearchPage);
	}

	[Test]
	public async Task When_Root_Favorites_Selected()
	{
		InitTestSection(TestSections.Apps_Chefs);

		await NavToRootPage(FavoritesPage);
	}

	[Test]
	public async Task When_Favorites_Selected_MyRecipes_Default()
	{
		InitTestSection(TestSections.Apps_Chefs);

		await NavToRootPage(FavoritesPage);

		var tabs = App.Marked("FavoritesTabBar");
		//Make sure we default to the Home Tab (first tab)
		App.WaitForDependencyPropertyValue(tabs, "SelectedIndex", "0");
		App.WaitForElement("FavoritesRecipeDetails");
	}

	[Test]
	public async Task When_Favorites_Selected_MyCookbooks()
	{
		InitTestSection(TestSections.Apps_Chefs);

		await NavToRootPage(FavoritesPage);

		await App.SelectListViewIndexAndWait("FavoritesTabBar", "1", "CookbookDetailsButton");
	}

	[Test]
	[TestCase(HomePage, new string[] {RecipeDetails})]
	[TestCase(SearchPage, new string[] {RecipeDetails})]
	[TestCase(FavoritesPage, new string[] {RecipeDetails})]
	[TestCase(HomePage, new string[] {RecipeDetails, LiveCooking})]
	[TestCase(SearchPage, new string[] {RecipeDetails, LiveCooking})]
	[TestCase(FavoritesPage, new string[] {RecipeDetails, LiveCooking})]
	public async Task When_RootPage_Nav_Forward(string startPage, string[] routes, bool goBack = true)
	{
		InitTestSection(TestSections.Apps_Chefs);

		
		await NavToRootPage(startPage);

		await NavToComplexRoute(startPage, routes, goBack);
	}


	[Test]
	public async Task When_Multi_Stacked_Root_Pages()
	{
		InitTestSection(TestSections.Apps_Chefs);

		await NavToRootPage(HomePage);

		foreach (var tab in new[] { HomePage, SearchPage, FavoritesPage })
		{
			await App.SelectListViewIndexAndWait("MainTabs", GetTabIndex(tab), $"{tab}NavigationBar");
			await NavToComplexRoute(tab, [RecipeDetails, LiveCooking], goBack: false);
		}

		foreach (var tab in new[] { HomePage, SearchPage, FavoritesPage })
		{
			await App.SelectListViewIndexAndWait("MainTabs", GetTabIndex(tab), $"{tab}LiveCookingNavigationBar");
			await NavigateBack(new Stack<string>([tab, RecipeDetails, LiveCooking]), tab);
		}

	}

	private async Task NavToComplexRoute(string startPage, string[] routes, bool goBack = true)
	{
		var backStack = new Stack<string>([startPage]);
		foreach (var page in routes)
		{
			App.WaitThenTap(startPage + page);
			var recipeName = App.Marked($"{startPage}{page}RecipeName");
			App.WaitForDependencyPropertyValue(recipeName, "Text", startPage);
			App.WaitForElement($"{startPage}{page}NavigationBar");

			backStack.Push(page);
		}

		if (!goBack) return;

		await NavigateBack(backStack, startPage);
	}

	private async Task NavigateBack(Stack<string> backStack, string startPage)
	{
		while (backStack.Count > 0)
		{
			var page = backStack.Pop();
			//App.Marked($"{page}BackButton")
			//.IsVisible()
			//.Should()
			//.BeTrue("Back button should be visible");
			
			if (backStack.TryPeek(out var backPage))
			{
				backPage = backPage == startPage ? string.Empty : backPage;
				await App.TapAndWait($"{startPage}{page}BackButton", $"{startPage}{backPage}NavigationBar");

				if (!backPage.IsNullOrWhiteSpace())
				{
					App.WaitForText($"{startPage}{backPage}RecipeName", startPage);
				}
			}
		}
	}

	private async Task NavToRootPage(string rootPage)
	{
		var index = GetTabIndex(rootPage);

		App.WaitThenTap("ShowAppButton");
		App.WaitThenTap("NextButton");
		App.WaitThenTap("LoginButton");

		await App.SelectListViewIndexAndWait("MainTabs", index, $"{rootPage}NavigationBar");
	}

	private static string GetTabIndex(string rootPage) => rootPage switch
	{
		HomePage => "0",
		SearchPage => "1",
		FavoritesPage => "2",
		_ => throw new ArgumentOutOfRangeException(nameof(rootPage), rootPage, null)
	};
}
