namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationTwoViewModel (INavigator Navigator, IWritableOptions<PageNavigationSettings> Settings)
{
	public async Task GoToThree()
	{
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
		await Navigator.NavigateViewModelAsync<PageNavigationThreeViewModel>(this);
	}

	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}
