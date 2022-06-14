namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationTwoViewModel (INavigator Navigator)
{
	public async Task GoToThree()
	{
		await Navigator.NavigateViewModelAsync<PageNavigationThreeViewModel>(this);
	}

	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}
