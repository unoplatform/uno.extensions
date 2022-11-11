namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationFourViewModel(INavigator Navigator, IDispatcher Dispatcher, IWritableOptions<PageNavigationSettings> Settings)
	: BasePageNavigationViewModel(Dispatcher)
{
	public async void GoToFive()
	{
#if !__WASM__
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
#endif
		await Navigator.NavigateViewModelAsync<PageNavigationFiveViewModel>(this);
	}

	public async void GoBack()
	{
		await Navigator.GoBack(this);
	}
}
