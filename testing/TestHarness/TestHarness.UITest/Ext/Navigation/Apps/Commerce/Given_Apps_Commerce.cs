namespace TestHarness.UITest;

public class Given_Apps_Commerce : NavigationTestBase
{
	[Test]
	public async Task When_Commerce_Backbutton()
	{
		InitTestSection(TestSections.Apps_Commerce);

		App.WaitThenTap("ShowAppButton");

		// Select the narrow layout
		App.WaitThenTap("NarrowButton");

		// Make sure the app has loaded
		App.WaitElement("LoginNavigationBar");

		// Login
		App.WaitThenTap("LoginButton");

		// Select Deals tab

		await App.TapAndWait("DealsTabBarItem", "DealsNavigationBar");

		// Select a deal
		await App.TapAndWait("DealsPage_DealsTabBarItem", "DealsListView");

		await App.SelectListViewIndexAndWait("DealsListView", "1", "ProductDetailsNavigationBar");

		// Go back to list of deals
		await App.TapAndWait("DetailsBackButton", "DealsListView");


		// Select another deal
		await App.SelectListViewIndexAndWait("DealsListView", "2", "ProductDetailsNavigationBar");

		// Go back to list of deals
		await App.TapAndWait("DetailsBackButton", "DealsListView");

	}

	[Test]
	public async Task When_Commerce_Responsive()
	{
		InitTestSection(TestSections.Apps_Commerce);

		App.WaitThenTap("ShowAppButton");

		// Select the narrow layout
		App.WaitThenTap("NarrowButton");

		// Make sure the app has loaded
		App.WaitElement("LoginNavigationBar");

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
		App.WaitElement("LoginNavigationBar");

		// Login
		App.WaitThenTap("LoginButton");


		/// Tap through each navigation view item

		await App.TapAndWait("DealsNavigationViewItem", "DealsNavigationBar");

		await App.TapAndWait("ProductsNavigationViewItem", "ProductsNavigationBar");

		await App.TapAndWait("ProfileNavigationViewItem", "ProfileNavigationBar");


		// Select a deal
		await App.TapAndWait("DealsNavigationViewItem", "DealsListView");

		await App.SelectListViewIndexAndWait("DealsListView", "2", "ProductDetailsNavigationBar");
		App.WaitElement("DealsNavigationBar");


		// Select a product
		await App.TapAndWait("ProductsNavigationViewItem", "ProductsListView");

		await App.SelectListViewIndexAndWait("ProductsListView", "1", "ProductDetailsNavigationBar");
		App.WaitElement("ProductsNavigationBar");

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
		App.WaitElement("LoginNavigationBar");

		// Login
		App.WaitThenTap("LoginButton");


		/// Tap through each navigation view item
		await App.TapAndWait("DealsNavigationViewItem", "DealsNavigationBar");
		var dealsId = App
			.MarkedAnywhere("DealsViewModelIdTextBlock")
			.WaitUntilExists()
			.GetDependencyPropertyValue<string>("Text");
		dealsId.Should().NotBeNull();

		await App.TapAndWait("ProductsNavigationViewItem", "ProductsNavigationBar");
		var productsId = App
			.MarkedAnywhere("ProductsViewModelIdTextBlock")
			.WaitUntilExists()
			.GetDependencyPropertyValue<string>("Text");
		productsId.Should().NotBeNull();

		await App.TapAndWait("ProfileNavigationViewItem", "ProfileNavigationBar");

		// Now go back to Deals and Products page and validate the Ids are the same (ie same instance of the viewmodel)
		await App.TapAndWait("DealsNavigationViewItem", "DealsNavigationBar");
		var newDealsId = App
			.MarkedAnywhere("DealsViewModelIdTextBlock")
			.WaitUntilExists()
			.GetDependencyPropertyValue<string>("Text");
		newDealsId.Should().NotBeNull();
		newDealsId.Should().BeEquivalentTo(dealsId);

		await App.TapAndWait("ProductsNavigationViewItem", "ProductsNavigationBar");
		var newProductsId = App
			.MarkedAnywhere("ProductsViewModelIdTextBlock")
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
		App.WaitElement("LoginNavigationBar");

		// Login
		App.WaitThenTap("LoginButton");


		/// Tap through each navigation view item
		await App.TapAndWait("DealsNavigationViewItem", "DealsNavigationBar");

		await App.TapAndWait("ProductsNavigationViewItem", "ProductsNavigationBar");

		await App.TapAndWait("FirstProductButton", "ProductDetailsNavigationBar");

		await App.TapAndWait("DealsNavigationViewItem", "DealsNavigationBar");

		await App.TapAndWait("ProductsNavigationViewItem", "ProductsNavigationBar");

		await App.TapAndWait("FirstProductBackgroundButton", "ProductDetailsNavigationBar");

		await App.TapAndWait("DealsNavigationViewItem", "DealsNavigationBar");
	}
}
