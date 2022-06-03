namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationThreeViewModel(INavigator Navigator)
{
	public async Task GoToFour()
	{
		await Navigator.NavigateViewModelAsync<PageNavigationFourViewModel>(this);
	}

	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}
