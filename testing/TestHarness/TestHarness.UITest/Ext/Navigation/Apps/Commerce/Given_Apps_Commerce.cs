namespace TestHarness.UITest;

public class Given_Apps_Commerce : NavigationTestBase
{
	[Test]
	[Ignore("https://github.com/unoplatform/uno.extensions/issues/626")]
	public async Task When_Commerce_Responsive()
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
		await App.TapAndWait("DealsTabBarItem", "DealsListView");

		await App.SelectListViewIndexAndWait("DealsListView", "1", "ProductDetailsNavigationBar");

		await App.TapAndWait("DetailsBackButton", "DealsNavigationBar");

		// Select a product
		await App.TapAndWait("ProductsTabBarItem", "ProductsListView");

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
		await App.TapAndWait("DealsNavigationViewItem", "DealsListView");

		await App.SelectListViewIndexAndWait("DealsListView", "2", "ProductDetailsNavigationBar");
		App.WaitForElement("DealsNavigationBar");


		// Select a product
		await App.TapAndWait("ProductsNavigationViewItem", "ProductsListView");

		await App.SelectListViewIndexAndWait("ProductsListView", "1", "ProductDetailsNavigationBar");
		App.WaitForElement("ProductsNavigationBar");

		// Log out
		App.WaitThenTap("ProfileNavigationViewItem");
		await App.TapAndWait("LogoutButton", "LoginButton");


	}

	[Test]
	public async Task When_ViewModelInstance()
	{
		InitTestSection(TestSections.Apps_Commerce);

		App.WaitThenTap("ShowAppButton");

		// Select the narrow layout
		App.WaitThenTap("WideButton");

		// Make sure the app has loaded
		App.WaitForElement("LoginNavigationBar");

		// Login
		App.WaitThenTap("LoginButton");


		/// Tap through each navigation view item
		await App.TapAndWait("DealsNavigationViewItem", "DealsNavigationBar");
		var dealsId = App
			.Marked("DealsViewModelIdTextBlock")
			.WaitUntilExists()
			.GetDependencyPropertyValue<string>("Text");
		dealsId.Should().NotBeNull();

		await App.TapAndWait("ProductsNavigationViewItem", "ProductsNavigationBar");
		var productsId = App
			.Marked("ProductsViewModelIdTextBlock")
			.WaitUntilExists()
			.GetDependencyPropertyValue<string>("Text");
		productsId.Should().NotBeNull();

		await App.TapAndWait("ProfileNavigationViewItem", "ProfileNavigationBar");

		// Now go back to Deals and Products page and validate the Ids are the same (ie same instance of the viewmodel)
		await App.TapAndWait("DealsNavigationViewItem", "DealsNavigationBar");
		var newDealsId = App
			.Marked("DealsViewModelIdTextBlock")
			.WaitUntilExists()
			.GetDependencyPropertyValue<string>("Text");
		newDealsId.Should().NotBeNull();
		newDealsId.Should().BeEquivalentTo(dealsId);

		await App.TapAndWait("ProductsNavigationViewItem", "ProductsNavigationBar");
		var newProductsId = App
			.Marked("ProductsViewModelIdTextBlock")
			.WaitUntilExists()
			.GetDependencyPropertyValue<string>("Text");
		newProductsId.Should().NotBeNull();
		newProductsId.Should().BeEquivalentTo(productsId);

	}


	[Test]
	public async Task When_BackgroundThread()
	{
		InitTestSection(TestSections.Apps_Commerce);

		App.WaitThenTap("ShowAppButton");

		// Select the narrow layout
		App.WaitThenTap("WideButton");

		// Make sure the app has loaded
		App.WaitForElement("LoginNavigationBar");

		// Login
		App.WaitThenTap("LoginButton");


		/// Tap through each navigation view item
		await App.TapAndWait("DealsNavigationViewItem", "DealsNavigationBar");

		await App.TapAndWait("ProductsNavigationViewItem", "ProductsNavigationBar");

		await App.TapAndWait("FirstProductButton", "ProductDetailsNavigationBar"); // Navigation by product data type finds the dealsproducts route first!

		await App.TapAndWait("DetailsBackButton", "DealsNavigationBar");

		await App.TapAndWait("ProductsNavigationViewItem", "ProductsNavigationBar");

		await App.TapAndWait("FirstProductBackgroundButton", "ProductDetailsNavigationBar"); // Navigation by product data type finds the dealsproducts route first!

		await App.TapAndWait("DetailsBackButton", "DealsNavigationBar");
	}
}
