﻿namespace TestHarness.UITest;

public class Given_AddressBar : NavigationTestBase
{
	[Test]
	public async Task When_AddressBar_HomePage_Wont_Navigate_Twice()
	{
		InitTestSection(TestSections.Navigation_AddressBar);

		App.WaitElement("TbInstanceCountProperty");

		var intanceCount = App.GetText("TbInstanceCountProperty");

		Assert.AreEqual("1", intanceCount);
	}

	[Test]
	[ActivePlatforms(Platform.Browser)]
	public void When_AddressBar_SecondPage_Query_Displayed()
	{
		InitTestSection(TestSections.Navigation_AddressBar);

		App.WaitThenTap("AddressBarSecondButton");
		AssertAddressBarUrl("AddressBarSecondPage", "?QueryUser.Id");

		//Ensure query parameters were cleared
		App.WaitThenTap("AddressBarSecondPageGoBack");
		AssertAddressBarUrl("AddressBarHomePage", "?QueryUser.Id", false);
	}

	[Test]
	[ActivePlatforms(Platform.Browser)]
	public void When_AddressBar_Nested_Navigated_Back_Params_Clear()
	{
		//Navigation starts with `Navigator.NavigateViewModelAsync<T>()`
		InitTestSection(TestSections.Navigation_AddressBar_Nested);
		Navigate_And_Clear_Params();
	}

	[Test]
	[ActivePlatforms(Platform.Browser)]
	public void When_AddressBar_Nested_Default_Navigated_Back_Params_Clear()
	{
		//Navigation starts with `IsDefault: true` on HostInit
		InitTestSection(TestSections.Navigation_AddressBar_Nested_Default);
		Navigate_And_Clear_Params();
	}

	private void Navigate_And_Clear_Params()
	{
		App.WaitElement("AddressBarRootPageNavigationView");

		//Goes to HomePage
		App.WaitThenTap("AddressBarRootPageHomeNavItem");

		//Goes to SecondPage
		App.WaitThenTap("AddressBarSecondButton");
		AssertAddressBarUrl("AddressBarSecondPage", "?QueryUser.Id");

		//Goes back to HomePage
		App.WaitThenTap("AddressBarSecondPageGoBack");
		AssertAddressBarUrl("AddressBarHomePage", "?QueryUser.Id", false);

		//Goes to CoffeePage
		App.WaitThenTap("AddressBarRootPageCoffeeNavItem");
		AssertAddressBarUrl("AddressBarCoffeePage", "?QueryUser.Id", false);

		//Goes to HomePage one more time
		App.WaitThenTap("AddressBarRootPageHomeNavItem");
		AssertAddressBarUrl("AddressBarHomePage", "?QueryUser.Id", false);

		//Goes to SecondPage to ensure params are added
		App.WaitThenTap("AddressBarSecondButton");
		AssertAddressBarUrl("AddressBarSecondPage", "?QueryUser.Id");

		//Goes back to HomePage to ensure params are cleared
		App.WaitThenTap("AddressBarSecondPageGoBack");
		AssertAddressBarUrl("AddressBarHomePage", "?QueryUser.Id", false);
	}

	[Test]
	[ActivePlatforms(Platform.Browser)]
	public void When_PageNavigationNavigateRootUpdateUrl()
	{
		InitTestSection(TestSections.Navigation_PageNavigation);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitThenTap("OnePageGetUrlFromBrowser");
		App.WaitElement("OnePageTxtUrlFromBrowser");
		var urlBefore = App.GetText("OnePageTxtUrlFromBrowser");

		App.WaitThenTap("OnePageToTwoPageButton");

		App.WaitThenTap("TwoPageBackButton");

		App.WaitThenTap("OnePageGetUrlFromBrowser");
		App.WaitElement("OnePageTxtUrlFromBrowser");
		var urlAfter = App.GetText("OnePageTxtUrlFromBrowser");

		Assert.AreEqual(urlBefore, urlAfter);
	}

	[Test]
	[ActivePlatforms(Platform.Browser)]
	public void When_PageNavigationNavigateQueryParamsDependsOn()
	{
		InitTestSection(TestSections.Navigation_PageNavigationRegistered);

		App.WaitThenTap("ShowOnePageButton");

		App.WaitThenTap("OnePageToTwoPageWithDataButton");

		AssertAddressBarUrl("SecondPage", "Value=0");

		App.WaitThenTap("TwoPageToThreePageViewModelButton");

		AssertAddressBarUrl("ThirdPage", "Value=0");
	}

	[Test]
	[ActivePlatforms(Platform.Browser)]
	public void When_AddressBar_Navigating_Through_NavItems_AddressBar_Updates()
	{
		InitTestSection(TestSections.Navigation_AddressBar_Nested);

		App.WaitElement("AddressBarRootPageNavigationView");

		// Sequencial check
		App.WaitThenTap("AddressBarRootPageCoffeeNavItem");
		AssertAddressBarUrl("AddressBarCoffeePage", "AddressBarCoffee");

		App.WaitThenTap("AddressBarRootPageHomeNavItem");
		AssertAddressBarUrl("AddressBarHomePage", "AddressBarHome");

		App.WaitThenTap("AddressBarRootPageSecondNavItem");
		AssertAddressBarUrl("AddressBarSecondPage", "AddressBarSecond");

		// Non-Sequencial check
		App.WaitThenTap("AddressBarRootPageHomeNavItem");
		AssertAddressBarUrl("AddressBarHomePage", "AddressBarHome");

		App.WaitThenTap("AddressBarRootPageSecondNavItem");
		AssertAddressBarUrl("AddressBarSecondPage", "AddressBarSecond");

		App.WaitThenTap("AddressBarRootPageHomeNavItem");
		AssertAddressBarUrl("AddressBarHomePage", "AddressBarHome");

		App.WaitThenTap("AddressBarRootPageCoffeeNavItem");
		AssertAddressBarUrl("AddressBarCoffeePage", "AddressBarCoffee");

		App.WaitThenTap("AddressBarRootPageSecondNavItem");
		AssertAddressBarUrl("AddressBarSecondPage", "AddressBarSecond");

		App.WaitThenTap("AddressBarRootPageHomeNavItem");
		AssertAddressBarUrl("AddressBarHomePage", "AddressBarHome");

		App.WaitThenTap("AddressBarRootPageSecondNavItem");
		AssertAddressBarUrl("AddressBarSecondPage", "AddressBarSecond");
	}

	[Test]
	public async Task When_Chefs_Navigating_Through_TabBarItems_AddressBar_Updates()
	{
		InitTestSection(TestSections.Apps_Chefs);

		App.WaitThenTap("ShowAppButton");
		App.WaitThenTap("NextButton");
		App.WaitThenTap("LoginButton");

		// MainTabs

		App.WaitThenTap("SearchTabBarItem");
		AssertAddressBarUrl("ChefsSearchPage", "ChefsSearch");

		App.WaitThenTap("HomeTabBarItem");
		AssertAddressBarUrl("ChefsHomePage", "ChefsHome");

		App.WaitThenTap("FavoriteTabBarItem");
		AssertAddressBarUrl("ChefsFavoriteRecipesPage", "ChefsFavoriteRecipes");

		App.WaitThenTap("SearchTabBarItem");
		AssertAddressBarUrl("ChefsSearchPage", "ChefsSearch");

		App.WaitThenTap("HomeTabBarItem");
		AssertAddressBarUrl("ChefsHomePage", "ChefsHome");

		// Nested Tabs

		App.WaitThenTap("FavoriteTabBarItem");
		AssertAddressBarUrl("ChefsFavoriteRecipesPage", "MyRecipes");

		App.WaitThenTap("MyCookbooksFavoritesTabBar");
		AssertAddressBarUrl("ChefsFavoriteRecipesPage", "MyCookbooks");

		App.WaitThenTap("MyRecipesFavoritesTabBar");
		AssertAddressBarUrl("ChefsFavoriteRecipesPage", "MyRecipes");
	}

	private void AssertAddressBarUrl(string pagePrefix, string contains, bool assertContains = true)
	{
		App.WaitThenTap($"{pagePrefix}GetUrlFromBrowser");

		var url = App.GetText($"{pagePrefix}TxtUrlFromBrowser");

		if (assertContains)
		{
			StringAssert.Contains(url, contains);
		}
		else
		{
			Assert.IsFalse(url.Contains(contains));
		}
	}
}
