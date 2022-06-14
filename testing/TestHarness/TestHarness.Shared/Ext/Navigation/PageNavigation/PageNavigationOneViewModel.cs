namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationOneViewModel (INavigator Navigator)
{
	public async Task GoToTwo()
	{
		await Navigator.NavigateViewModelAsync<PageNavigationTwoViewModel>(this);
	}
}
