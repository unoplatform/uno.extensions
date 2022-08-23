namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationFourViewModel(INavigator Navigator, IWritableOptions<PageNavigationSettings> Settings)
{
	public async void GoToFive()
	{
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
		await Navigator.NavigateViewModelAsync<PageNavigationFiveViewModel>(this);
	}

	public async void GoBack()
	{
		await Navigator.GoBack(this);
	}
}
