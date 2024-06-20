namespace TestHarness.Ext.Navigation.NavigationView;

[ReactiveBindable(false)]
public partial class NavigationViewDataBoundViewModel : ObservableObject
{
	[ObservableProperty]
	private string selectedNavigationItem = "Deals";

	public string[] NavigationItems { get; } = ["Products", "Deals"];
	public string[] FooterItems { get; } = ["Profile"];

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

