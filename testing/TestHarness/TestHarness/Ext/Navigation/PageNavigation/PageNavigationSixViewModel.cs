namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationSixViewModel (INavigator Navigator, IDispatcher Dispatcher, IWritableOptions<PageNavigationSettings> Settings, Widget Data)
	: BasePageNavigationViewModel(Dispatcher)
{
	public async void GoToSeven()
	{
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
		await Navigator.NavigateViewModelAsync<PageNavigationSevenViewModel>(this);
	}
}
