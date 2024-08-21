namespace TestHarness.Ext.Navigation.RoutesNavigation;

public record ShellViewModel
{
	public INavigator? Navigator { get; init; }


	public ShellViewModel(INavigator navigator)
	{
		Navigator = navigator;

		_ = Start();
	}

	public async Task Start()
	{
		await Navigator!.NavigateViewAsync<HomePage>(this);
		
	}
}
