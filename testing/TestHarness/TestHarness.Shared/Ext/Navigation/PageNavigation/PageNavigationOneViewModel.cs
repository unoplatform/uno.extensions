namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationOneViewModel (INavigator Navigator, IWritableOptions<PageNavigationSettings> Settings)
{
	public async Task GoToTwo()
	{
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
		await Navigator.NavigateViewModelAsync<PageNavigationTwoViewModel>(this);
	}

}
