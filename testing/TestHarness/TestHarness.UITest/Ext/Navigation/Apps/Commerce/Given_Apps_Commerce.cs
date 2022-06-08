namespace TestHarness.UITest;

public class Given_Apps_Commerce : NavigationTestBase
{
	[Test]
	public async Task When_Responsive()
	{
		InitTestSection(TestSections.Apps_Commerce);

		App.WaitThenTap("ShowAppButton");

		// Select the narrow layout
		App.WaitThenTap("NarrowButton");

		// Make sure the app has loaded
		App.WaitForElement("LoginNavigationBar");

		// Login
		App.WaitThenTap("LoginButton");

		/// Tap through each tab bar item

		await App.TapAndWait("DealsTabBarItem", "DealsNavigationBar");

		await App.TapAndWait("ProductsTabBarItem", "ProductsNavigationBar");

		await App.TapAndWait("ProfileTabBarItem", "ProfileNavigationBar");

		// Select a deal
		await App.TapAndWait("DealsTabBarItem", "DealsNavigationBar");

		await App.SelectListViewIndexAndWait("DealsListView", "1", "ProductDetailsNavigationBar");

		await App.TapAndWait("DetailsBackButton", "DealsNavigationBar");

		// Select a product
		await App.TapAndWait("ProductsTabBarItem", "ProductsNavigationBar");

		await App.SelectListViewIndexAndWait("ProductsListView", "2", "ProductDetailsNavigationBar");

		await App.TapAndWait("DetailsBackButton", "ProductsNavigationBar");


		// Log out
		App.WaitThenTap("ProfileTabBarItem");
		await App.TapAndWait("LogoutButton", "LoginButton");



		// Select the wide layout
		App.WaitThenTap("WideButton");

		// Make sure the app has loaded
		App.WaitForElement("LoginNavigationBar");

		// Login
		App.WaitThenTap("LoginButton");


		/// Tap through each navigation view item

		await App.TapAndWait("DealsNavigationViewItem","DealsNavigationBar");

		await App.TapAndWait("ProductsNavigationViewItem","ProductsNavigationBar");

		await App.TapAndWait("ProfileNavigationViewItem","ProfileNavigationBar");


		// Select a deal
		await App.TapAndWait("DealsNavigationViewItem","DealsNavigationBar");

		await App.SelectListViewIndexAndWait("DealsListView", "2", "ProductDetailsNavigationBar");
		App.WaitForElement("DealsNavigationBar");


		// Select a product
		await App.TapAndWait("ProductsNavigationViewItem","ProductsNavigationBar");

		await App.SelectListViewIndexAndWait("ProductsListView", "1", "ProductDetailsNavigationBar");
		App.WaitForElement("ProductsNavigationBar");

		// Log out
		App.WaitThenTap("ProfileNavigationViewItem");
		await App.TapAndWait("LogoutButton", "LoginButton");


	}

}
