namespace Playground.ViewModels;

public partial class BasicViewModel
{
	private readonly INavigator _navigator;
	public BasicViewModel(INavigator navigator)
	{
		_navigator = navigator;
	}

	public async void Close()
	{
		await _navigator.NavigateBackAsync(this);
	}
	public async void CloseWithData()
	{
		await _navigator.NavigateBackWithResultAsync(this, data:new Widget { Name="Dialog Widget"});
	}
}
