namespace TestHarness.Ext.Navigation.Apps.Regions;

public class RegionsShellModel
{
	private readonly INavigator _navigator;

	public RegionsShellModel(
		INavigator navigator)
	{
		_navigator = navigator;
		_ = Start();
	}

	public async Task Start()
	{
		await _navigator.NavigateViewAsync<RegionsHomePage>(this);
	}
}
