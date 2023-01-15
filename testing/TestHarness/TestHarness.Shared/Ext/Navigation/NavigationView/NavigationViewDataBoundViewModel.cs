namespace TestHarness.Ext.Navigation.NavigationView;

public partial class NavigationViewDataBoundViewModel : ObservableObject
{
	[ObservableProperty]
	private string selectedNavigationItem = "Deals";

	public string[] NavigationItems { get; } = new string[] { "Products", "Deals", "Profile" };

	public void SelectProfile()
	{
		SelectedNavigationItem = "Profile";
	}

	public void SelectProducts()
	{
		SelectedNavigationItem = "Products";
	}

	public void SelectDeals()
	{
		SelectedNavigationItem = "Deals";
	}
}

