namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationFourViewModel (INavigator Navigator)
{
	public async Task GoToFive()
{
	await Navigator.NavigateViewModelAsync<PageNavigationFiveViewModel>(this);
}

public async Task GoBack()
{
	await Navigator.GoBack(this);
}
}
