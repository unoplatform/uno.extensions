namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationThreeViewModel(INavigator Navigator, IDispatcher Dispatcher, IWritableOptions<PageNavigationSettings> Settings)
	: BasePageNavigationViewModel(Dispatcher)
{
	public async void GoToFour()
	{
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
		await Navigator.NavigateViewModelAsync<PageNavigationFourViewModel>(this);
	}

	public async void GoBack()
	{
		await Navigator.GoBack(this);
	}
}
