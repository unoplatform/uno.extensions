namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationFiveViewModel(INavigator Navigator)
{
	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}
