namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationThreeViewModel(INavigator Navigator, IWritableOptions<PageNavigationSettings> Settings)
{
	public async Task GoToFour()
	{
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
		await Navigator.NavigateViewModelAsync<PageNavigationFourViewModel>(this);
	}

	public async Task GoBack()
	{
		await Navigator.GoBack(this);
	}
}
