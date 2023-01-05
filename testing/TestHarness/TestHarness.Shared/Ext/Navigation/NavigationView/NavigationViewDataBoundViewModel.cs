namespace TestHarness.Ext.Navigation.NavigationView;

public record NavigationViewDataBoundViewModel(INavigator Navigator)
{
	public string[] NavigationItems { get; } = new string[] { "Products", "Deals", "Profile" };

}

