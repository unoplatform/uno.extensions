namespace Playground.ViewModels;

public class BasicViewModel
{
	private readonly INavigator _navigator;
	public BasicViewModel(INavigator navigator)
	{
		_navigator = navigator;
	}

	public async Task Close()
	{
		await _navigator.NavigateBackAsync(this);
	}
	public async Task CloseWithData()
	{
		await _navigator.NavigateBackWithResultAsync(this, data:new Widget { Name="Dialog Widget"});
	}
}
