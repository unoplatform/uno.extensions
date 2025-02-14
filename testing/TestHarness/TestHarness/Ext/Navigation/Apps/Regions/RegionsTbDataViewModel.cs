namespace TestHarness.Ext.Navigation.Apps.Regions;

public record RegionsTbDataPageViewModel
{
	private INavigator _navigator;

	public RegionsTbDataPageViewModel(INavigator navigator)
	{
		_navigator = navigator;

		Entity = new("TabBar Entity");
	}

	public RegionEntityData Entity { get; set; }
}
