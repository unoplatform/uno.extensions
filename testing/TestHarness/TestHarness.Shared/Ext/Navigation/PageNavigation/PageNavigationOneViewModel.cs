using System.Collections.Immutable;

namespace TestHarness.Ext.Navigation.PageNavigation;

public record PageNavigationOneViewModel(IServiceProvider Services, INavigator Navigator, IWritableOptions<PageNavigationSettings> Settings)
{
	public async void GoToTwo()
	{
		await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add(this.GetType().Name) });
		await Navigator.NavigateViewModelAsync<PageNavigationTwoViewModel>(this);
	}

	public async void SettingsWriteTest()
	{
		var tasks = new List<Task>();
		for (int i = 0; i < 5; i++)
		{
			tasks.Add(Task.Run(()=>ReadWriteTest()));
		}
		await Task.WhenAll(tasks);
	}

	private async Task ReadWriteTest()
	{
		var rnd = new Random();
		await Settings.UpdateAsync(s => s with { PagesVisited = ImmutableList<string>.Empty });
		var settings = Settings.Value;
		for (int i = 0; i < 50; i++)
		{
			await Settings.UpdateAsync(s => s with { PagesVisited = s.PagesVisited.Add($"Random {rnd.NextDouble() * 10000}") });
			settings = Settings.Value;

			var accessor = Services.GetRequiredService<IWritableOptions<PageNavigationSettings>>();
			settings = accessor.Value;
		}

	}

}
